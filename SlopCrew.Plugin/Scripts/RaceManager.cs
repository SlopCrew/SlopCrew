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

namespace SlopCrew.Plugin.Scripts {
    public class RaceManager : MonoBehaviour, IStatefullApp {
        public static RaceManager? Instance;
        private IEnumerable<(string? playerName, float time)> rank = new List<(string? playerName, float time)>();
        private RaceState state;
        private float time;
        private Queue<MapPin> mapPins = new Queue<MapPin>();
        private bool shouldLoadRaceStageASAP = false;
        private bool hasAdditionRaceConfigToLoad = false;
        private DateTime showRankTime;
        private const int MAX_TIME_TO_SHOW_RANKING_SECS = 20;
        private RaceConfig? currentRaceConfig;
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


        private void ShowTimer() {
            var uiManager = Core.Instance.UIManager;
            var gameplayUI = Traverse.Create(uiManager).Field<GameplayUI>("gameplay").Value;
            gameplayUI.challengeGroup.SetActive(true);

            gameplayUI.timeLimitLabel.text = GetTimeFormatted().ToString();
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
                    return "Waiting for players\n to join race ...";
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
                    return "Ranking:\n" + string.Join("\n", rank.Select((r, i) => $"{i + 1} - {r.playerName} - {r.time}"));
                default:
                    return "";
            }
        }

        public void Awake() {
            DontDestroyOnLoad(gameObject);

            Instance = this;
            state = RaceState.None;
            this.gameObject.AddComponent<RaceVelocityModifier>();
        }

        public void Update() {
            if (state == RaceState.Racing || state == RaceState.Starting) {
                var currentPlayer = WorldHandler.instance.GetCurrentPlayer();
                if (currentPlayer != null) {
                    Traverse.Create(currentPlayer).Method("UpdateUIIndicatorData").GetValue();
                }

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
                    var currentMapPin = mapPins.Peek();
                    if (!currentMapPin.gameObject.activeSelf) {
                        currentMapPin.gameObject.SetActive(true);
                    }

                    if (GetTimeInSecs() > 3) {
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

                    if (remainingTime.TotalSeconds > MAX_TIME_TO_SHOW_RANKING_SECS) {
                        state = RaceState.None;
                    }

                    break;
            }
        }

        public bool IsInRace() {
            return state != RaceState.None && state != RaceState.ShowRanking;
        }

        public void OnRaceRequestResponse(bool isOk, RaceConfig raceConfig) {
            if (isOk) {
                Plugin.Log.LogInfo("Race request got accepted!");
                currentRaceConfig = raceConfig;

                time = 0;
                state = RaceState.WaitingForPlayers;
            } else {
                Plugin.Log.LogError("Race request got denied...");
                state = RaceState.None;
            }
        }

        internal void OnRaceInitialize() {
            if (state != RaceState.WaitingForPlayers) {
                Plugin.Log.LogWarning("Not waiting for a race");
                return;
            }

            Plugin.ShouldIgnoreInput = true;

            BaseModule instance = Traverse.Create(typeof(BaseModule)).Field("instance").GetValue<BaseModule>();
            Stage currentStage = Traverse.Create(instance).Field("currentStage").GetValue<Stage>();

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
            mapPins = new Queue<MapPin>(currentRaceConfig.MapPins.Count());

            foreach (var mapPin in currentRaceConfig.MapPins) {
                var pin = Mapcontroller.Instance.InstantiateRacePin(mapPin);

                pin.gameObject.SetActive(false);

                mapPins.Enqueue(pin);
            }

            var player = WorldHandler.instance.GetCurrentPlayer();
            player.tf.position = currentRaceConfig.StartPosition.ToMentalDeficiency();
            player.motor.SetVelocityTotal(Vector3.zero, Vector3.zero, Vector3.zero);
            player.tf.LookAt(mapPins.Peek().transform);
            player.boostCharge = 0;

            ////Respawn all boost pickups
            UnityEngine.Object.FindObjectsOfType<Pickup>()
                .Where((pickup) => pickup.pickupType is Pickup.PickUpType.BOOST_CHARGE || pickup.pickupType is Pickup.PickUpType.BOOST_BIG_CHARGE)
                .ToList()
                .ForEach((boost) => {
                    boost.SetPickupActive(true);
                });

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
            var currentPin = mapPins.Peek();
            if (currentPin != mapPin) {
                Plugin.Log.LogInfo("Wrong checkpoint ?");
                return false;
            }

            if (mapPins.Count > 0) {
                currentPin.gameObject.SetActive(false);

                mapPins.Dequeue();

                if (mapPins.Count > 0) {
                    var pin = mapPins.Peek();
                    pin.gameObject.SetActive(true);
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
            if (state != RaceState.WaitingForFullRanking) {
                Plugin.Log.LogInfo("Not waiting for race rank");
                return;
            }

            foreach (var (playerName, time) in rank) {
                Plugin.Log.LogInfo($"{playerName} finished in {time} seconds");
            }

            this.rank = rank;
            state = RaceState.ShowRanking;
            showRankTime = DateTime.UtcNow;
        }

        public IEnumerable<(string? playerName, float time)> GetRank() {
            return rank;
        }

        public bool HasStarted() {
            return state == RaceState.Racing;
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

        private string GetTimeFormatted() {
            var raceManager = RaceManager.Instance;

            var time = GetDeltaTime();
            var minutes = Mathf.FloorToInt(time / 60);
            var seconds = Mathf.FloorToInt(time % 60);
            var fraction = Mathf.FloorToInt((time * 100) % 100);

            return $"{minutes:00}:{seconds:00}:{fraction:00}";
        }

        private string GetTimeFrom(int from = 4) {
            var raceManager = RaceManager.Instance;

            var time = GetDeltaTime();
            var minutes = Mathf.FloorToInt(from - time);

            return $"{minutes:00}";
        }
    }
}
