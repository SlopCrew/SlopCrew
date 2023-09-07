using HarmonyLib;
using Reptile;
using SlopCrew.Common;
using SlopCrew.Common.Network.Serverbound;
using SlopCrew.Common.Race;
using SlopCrew.Plugin.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SlopCrew.Plugin.Scripts.Race {
    public class RaceManager : IStatefulApp {
        private const int MAX_TIME_TO_SHOW_RANKING_SECS = 20;

        private IEnumerable<(string? playerName, float time)> rank = new List<(string? playerName, float time)>();
        private RaceState state;
        private float time;
        private Queue<CheckpointPin> checkpointPins = new Queue<CheckpointPin>();
        private bool shouldLoadRaceStageASAP = false;
        private bool hasAdditionRaceConfigToLoad = false;
        private DateTime showRankTime;
        private DateTime willStart;
        private RaceConfig? currentRaceConfig;

        public RaceManager() {
            Core.OnUpdate += this.Update;
            state = RaceState.None;
        }

        public void Update() {
            if (state == RaceState.Racing || state == RaceState.Starting) {
                checkpointPins.Peek().UpdateUIIndicator();

                time += Time.deltaTime;
                if (state == RaceState.Racing)
                    ShowTimer();

                if (state == RaceState.Starting)
                    ShowTimerReverse();
            }

            switch (state) {
                case RaceState.WaitingForRace:
                    break;
                case RaceState.Starting:
                    var currentCheckpointPin = checkpointPins.Peek();
                    if (!currentCheckpointPin.Pin.gameObject.activeSelf) {
                        currentCheckpointPin.Activate();
                    }

                    if (GetTimeInSecs() > 2) {
                        Plugin.ShouldIgnoreInput = false;
                        state = RaceState.Racing;
                        time = 0;
                    }

                    break;
                case RaceState.Racing:

                    break;
                case RaceState.Finished:
                    OnRaceFinished();

                    break;
                case RaceState.ShowRanking:
                    var remainingTime = DateTime.UtcNow - showRankTime;

                    if (remainingTime.Seconds > MAX_TIME_TO_SHOW_RANKING_SECS) {
                        ResetAll();
                    }

                    break;
            }
        }

        public bool IsBusy() {
            return state != RaceState.None;
        }

        public bool IsInRace() {
            return state != RaceState.None && state != RaceState.ShowRanking;
        }

        public void OnRaceRequestResponse(bool isOk, RaceConfig raceConfig, string willStartTime) {
            if (isOk) {
                currentRaceConfig = raceConfig;
                time = 0;
                state = RaceState.WaitingForPlayers;
                willStart = DateTime.Parse(willStartTime);
            } else {
                Plugin.Log.LogError("Race request got denied...");
                ResetAll();
            }
        }

        public void OnRaceAborted() {
            Plugin.Log.LogInfo("Race aborted ! ");

            ResetAll();
        }

        public void OnRaceInitialize() {
            if (state != RaceState.WaitingForPlayers) {
                Plugin.Log.LogWarning("Not waiting for a race");
                return;
            }

            Plugin.ShouldIgnoreInput = true;

            var instance = Traverse.Create(typeof(BaseModule)).Field("instance").GetValue<BaseModule>();
            var currentStage = Traverse.Create(instance).Field("currentStage").GetValue<Stage>();

            if (currentRaceConfig.Stage.ToBRCStage() != currentStage) {
                Plugin.Log.LogInfo("Switching stage to " + currentRaceConfig.Stage);
                shouldLoadRaceStageASAP = true;
                state = RaceState.LoadingStage;

                return;
            }

            Plugin.Log.LogInfo("Stage is already loaded!");

            AdditionalRaceInitialization();
        }

        public void AdditionalRaceInitialization() {
            checkpointPins = new Queue<CheckpointPin>(currentRaceConfig!.MapPins.Count());

            foreach (var mapPin in currentRaceConfig.MapPins) {
                var checkpointPin = Mapcontroller.Instance.InstantiateRaceCheckPoint(mapPin.ToVector3());

                checkpointPin.Pin.gameObject.SetActive(false);
                checkpointPins.Enqueue(checkpointPin);
            }

            var player = WorldHandler.instance.GetCurrentPlayer();
            player.tf.position = currentRaceConfig.StartPosition.ToVector3().ToMentalDeficiency();
            player.motor.SetVelocityTotal(Vector3.zero, Vector3.zero, Vector3.zero);
            player.StopCurrentAbility();
            player.boostCharge = 0;

            ////Respawn all boost pickups
            UnityEngine.Object.FindObjectsOfType<Pickup>()
                .Where((pickup) => pickup.pickupType is Pickup.PickUpType.BOOST_CHARGE || pickup.pickupType is Pickup.PickUpType.BOOST_BIG_CHARGE)
                .ToList()
                .ForEach((boost) => {
                    boost.SetPickupActive(true);
                });

            Mapcontroller.Instance.UpdateOriginalPins(false);

            state = RaceState.WaitingForPlayersToBeReady;

            Plugin.NetworkConnection.SendMessage(new ServerboundReadyForRace());
        }

        public void OnRaceStart() {
            if (state != RaceState.WaitingForPlayersToBeReady) {
                Plugin.Log.LogWarning("Not waiting to start a race");
                return;
            }

            Plugin.ShouldIgnoreInput = true;
            state = RaceState.Starting;
        }

        public bool OnCheckpointReached(MapPin mapPin) {
            var currentCheckpointPin = checkpointPins.Peek();
            if (currentCheckpointPin.Pin != mapPin) {
                Plugin.Log.LogInfo("Wrong checkpoint ?");
                return false;
            }

            if (checkpointPins.Count > 0) {
                currentCheckpointPin.Deactivate();

                checkpointPins.Dequeue();

                if (checkpointPins.Count > 0) {
                    var checkpointPin = checkpointPins.Peek();
                    checkpointPin.Activate();
                } else {
                    state = RaceState.Finished;
                }

                return true;
            }

            return false;
        }

        private void OnRaceFinished() {
            state = RaceState.WaitingForFullRanking;

            Plugin.NetworkConnection.SendMessage(new ServerboundFinishedRace {
                Time = time
            });
        }

        public void OnRaceRank(IEnumerable<(string? playerName, float time)> rank) {
            foreach (var (playerName, time) in rank) {
                Plugin.Log.LogInfo($"{playerName} finished in {time} seconds");
            }

            this.rank = rank;
            state = RaceState.ShowRanking;
            showRankTime = DateTime.UtcNow;
        }

        public void OnStart() {
            if (IsInRace()) {
                Plugin.Log.LogDebug("You are already in a race !");
                return;
            }

            Plugin.NetworkConnection.SendMessage(new ServerboundEncounterRequest {
                EncounterType = EncounterType.RaceEncounter
            });

            state = RaceState.WaitingForRace;
        }

        private void ResetAll() {
            rank = new List<(string? playerName, float time)>();
            state = RaceState.None;
            time = 0;
            checkpointPins = new Queue<CheckpointPin>();
            shouldLoadRaceStageASAP = false;
            hasAdditionRaceConfigToLoad = false;
            showRankTime = DateTime.MinValue;
            currentRaceConfig = null;

            var uiManager = Core.Instance.UIManager;
            var gameplayUI = Traverse.Create(uiManager).Field<GameplayUI>("gameplay").Value;
            gameplayUI.challengeGroup.SetActive(false);
            gameplayUI.timeLimitLabel.text = "";

            Mapcontroller.Instance.UpdateOriginalPins(true);
        }


        private void ShowTimer() {
            var uiManager = Core.Instance.UIManager;
            var gameplayUI = Traverse.Create(uiManager).Field<GameplayUI>("gameplay").Value;
            gameplayUI.challengeGroup.SetActive(true);

            gameplayUI.timeLimitLabel.text = GetTimeFormatted(time).ToString();
        }

        private void ShowTimerReverse() {
            var uiManager = Core.Instance.UIManager;
            var gameplayUI = Traverse.Create(uiManager).Field<GameplayUI>("gameplay").Value;
            gameplayUI.challengeGroup.SetActive(true);

            gameplayUI.timeLimitLabel.text = GetTimeFrom().ToString();
        }

        public string GetLabel() {
            switch (state) {
                case RaceState.None:
                    return "Press right\nto enter in a race \n";
                case RaceState.WaitingForRace:
                    return "Waiting for a race ...";
                case RaceState.WaitingForPlayers:
                    return $"Waiting {(willStart - DateTime.UtcNow).Seconds} for players\n to join race ...";
                case RaceState.LoadingStage:
                    return "Loading race stage ...";
                case RaceState.WaitingForPlayersToBeReady:
                    return "Ready!\nWaiting for other players\n to be ready ...";
                case RaceState.Starting:
                case RaceState.Racing:
                    return "You definetely\nshouldn't be looking\nat your phone right now...";
                case RaceState.Finished:
                case RaceState.WaitingForFullRanking:
                    return "Waiting for full ranking ...";
                case RaceState.ShowRanking:
                    return "Ranking:\n" + string.Join("\n", rank.Select((r, i) => $"{i + 1} - {r.playerName} - {GetTimeFormatted(r.time)}"));
                default:
                    return "";
            }
        }

        public IEnumerable<(string? playerName, float time)> GetRank() {
            return rank;
        }

        public bool HasStarted() {
            return state == RaceState.Racing;
        }

        public bool IsStarting() {
            return state == RaceState.Starting;
        }

        public CheckpointPin? GetNextCheckpointPin() {
            if (checkpointPins == null || checkpointPins.Count == 0) {
                return null;
            }

            return checkpointPins.Peek();
        }

        public bool ShouldLoadRaceStageASAP() {
            return shouldLoadRaceStageASAP;
        }

        public void SetShouldLoadRaceStageASAP(bool shouldLoadRaceStageASAP) {
            this.shouldLoadRaceStageASAP = shouldLoadRaceStageASAP;
        }

        public bool HasAdditionRaceConfigToLoad() {
            return hasAdditionRaceConfigToLoad;
        }

        public void SetHasAdditionRaceConfigToLoad(bool hasAdditionRaceConfigToLoad) {
            this.hasAdditionRaceConfigToLoad = hasAdditionRaceConfigToLoad;
        }

        private int GetTimeInSecs() {
            return (int) time % 60;
        }

        public float GetDeltaTime() {
            return time;
        }

        public Stage GetStage() {
            return currentRaceConfig.Stage.ToBRCStage();
        }

        internal RaceState GetState() {
            return state;
        }

        private string GetTimeFormatted(float time) {
            var minutes = Mathf.FloorToInt(time / 60);
            var seconds = Mathf.FloorToInt(time % 60);
            var fraction = Mathf.FloorToInt(time * 100 % 100);
            return $"{minutes:00}:{seconds:00}:{fraction:00}";
        }

        private string GetTimeFrom(int from = 3) {
            var time = GetDeltaTime();
            var seconds = Mathf.FloorToInt(from - time);
            var ms = Mathf.FloorToInt((from - time) * 100 % 100);
            return $"{seconds:00}:{ms:00}";
        }
    }
}
