using HarmonyLib;
using Reptile;
using Reptile.Phone;
using SlopCrew.Common;
using SlopCrew.Common.Encounters;
using SlopCrew.Common.Network.Clientbound;
using SlopCrew.Common.Network.Serverbound;
using SlopCrew.Plugin.Encounters.Race;
using SlopCrew.Plugin.Extensions;
using SlopCrew.Plugin.UI.Phone;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Vector3 = System.Numerics.Vector3;

namespace SlopCrew.Plugin.Encounters;

public class SlopRaceEncounter : SlopEncounter {
    private Queue<RaceCheckpoint> checkpoints;
    private RaceState state = RaceState.Start;

    private Stopwatch timer = new();

    public SlopRaceEncounter(RaceEncounterConfigData configData) : base(configData) {
        var pins = configData.RaceConfig.MapPins
            .Select(x => {
                var checkpoint = new GameObject("RaceCheckpoint");
                checkpoint.tag = RaceCheckpoint.Tag;
                checkpoint.transform.position = x.ToMentalDeficiency();
                var component = checkpoint.AddComponent<RaceCheckpoint>();
                return component;
            })
            .ToList();

        this.checkpoints = new Queue<RaceCheckpoint>();
        foreach (var pin in pins) this.checkpoints.Enqueue(pin);

        var player = WorldHandler.instance.GetCurrentPlayer();
        player.tf.position = configData.RaceConfig.StartPosition.ToMentalDeficiency();
        player.motor.SetVelocityTotal(
            Vector3.Zero.ToMentalDeficiency(),
            Vector3.Zero.ToMentalDeficiency(),
            Vector3.Zero.ToMentalDeficiency()
        );
        player.StopCurrentAbility();
        player.boostCharge = 0;

        Plugin.ShouldIgnoreInput = true;

        // Respawn all boost pickups
        UnityEngine.Object.FindObjectsOfType<Pickup>()
            .Where(pickup => pickup.pickupType is Pickup.PickUpType.BOOST_CHARGE or Pickup.PickUpType.BOOST_BIG_CHARGE)
            .ToList()
            .ForEach(boost => { boost.SetPickupActive(true); });

        this.timer.Start();
        this.IsBusy = true;
    }

    protected override void Update() {
        var checkpoint = this.GetNextCheckpoint();
        if (checkpoint != null) checkpoint.UpdateUIIndicator();

        switch (this.state) {
            case RaceState.Start: {
                    if (this.timer.Elapsed.TotalSeconds > 3) {
                        this.state = RaceState.Race;
                        Plugin.ShouldIgnoreInput = false;
                        this.timer.Restart();
                    } else {
                        var secondsLeft = (int) Math.Ceiling(3 - this.timer.Elapsed.TotalSeconds);
                        var periods = new string('.', secondsLeft);
                        var countdown = secondsLeft.ToString(CultureInfo.CurrentCulture) + periods;
                        this.SetTimer(countdown);
                    }

                    var player = WorldHandler.instance.GetCurrentPlayer();
                    var phone = Traverse.Create(player).Field("phone").GetValue<Phone>();
                    var app = phone.GetAppInstance<AppSlopCrew>();
                    app.EndWaitingForRace();

                    break;
                }

            case RaceState.Race: {
                    this.SetTimer(this.TimeElapsed());
                    break;
                }

            case RaceState.Finish: {
                    break;
                }
        }
    }

    private void SetTimer(string? text) {
        var uiManager = Core.Instance.UIManager;
        var gameplay = Traverse.Create(uiManager).Field<GameplayUI>("gameplay").Value;

        if (text is null) {
            gameplay.challengeGroup.SetActive(false);
            return;
        }

        gameplay.challengeGroup.SetActive(true);
        gameplay.totalScoreLabel.text = string.Empty;
        gameplay.targetScoreLabel.text = string.Empty;
        gameplay.totalScoreTitleLabel.text = string.Empty;
        gameplay.targetScoreTitleLabel.text = string.Empty;
        gameplay.timeLimitLabel.text = text;
    }

    private string TimeElapsed() => this.NiceTimerString(this.timer.Elapsed.TotalSeconds);

    public bool IsStarting() => this.state == RaceState.Start;
    public bool IsWaitingForResults() => this.state == RaceState.Finish;

    public RaceCheckpoint? GetNextCheckpoint() {
        return this.checkpoints.Count > 0 ? this.checkpoints.Peek() : null;
    }

    public bool OnCheckpointReached(RaceCheckpoint checkpoint) {
        var nextPin = this.checkpoints.Peek();
        if (nextPin.Pin != checkpoint.Pin) return false;

        this.checkpoints.Dequeue();

        if (this.checkpoints.Count == 0) {
            this.state = RaceState.Finish;
            Plugin.ShouldIgnoreInput = false;

            Plugin.NetworkConnection.SendMessage(new ServerboundRaceFinish {
                Guid = this.Guid,
                Time = (float) this.timer.Elapsed.TotalSeconds
            });

            this.SetTimer(this.TimeElapsed());

            this.timer.Reset();
            this.timer.Stop();

            Core.Instance.AudioManager.PlaySfx(
                SfxCollectionID.EnvironmentSfx,
                AudioClipID.MascotUnlock
            );
        }

        return true;
    }

    public override void Dispose() {
        base.Dispose();

        foreach (var checkpointPin in this.checkpoints) {
            if (checkpointPin != null) {
                UnityEngine.Object.Destroy(checkpointPin.gameObject);
            }
        }
        this.checkpoints.Clear();

        this.SetTimer(null);
        Plugin.ShouldIgnoreInput = false;
    }

    public override void HandleEnd(ClientboundEncounterEnd encounterEnd) {
        base.HandleEnd(encounterEnd);

        var player = WorldHandler.instance.GetCurrentPlayer();
        var phone = Traverse.Create(player).Field("phone").GetValue<Phone>();
        var app = phone.GetAppInstance<AppSlopCrew>();
        var endData = encounterEnd.EndData as RaceEncounterEndData;

        var sorted = endData!.Rankings.OrderBy(x => x.Value).ToList();

        var str = "";
        foreach (var kvp in sorted) {
            var id = kvp.Key;
            var timeStr = "<color=white>: " + this.NiceTimerString(kvp.Value) + "\n";

            if (Plugin.PlayerManager.Players.TryGetValue(id, out var associatedPlayer)) {
                var name = PlayerNameFilter.DoFilter(associatedPlayer.SlopPlayer.Name);
                str += name + timeStr;
            } else {
                // Assume players not in our players list is us
                str += Plugin.SlopConfig.Username.Value + timeStr;
            }
        }
        app.RaceRankings = str.Trim();

        //It should already be done, but just in case
        app.EndWaitingForRace();
    }

    enum RaceState {
        Start,
        Race,
        Finish
    }
}
