using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.Collections;
using Microsoft.Extensions.DependencyInjection;
using Reptile;
using SlopCrew.Common;
using SlopCrew.Common.Proto;
using SlopCrew.Plugin.Encounters.Race;
using SlopCrew.Plugin.UI.Phone;
using UnityEngine;
using Vector3 = System.Numerics.Vector3;

namespace SlopCrew.Plugin.Encounters;

public class RaceEncounter : Encounter {
    private EncounterManager encounterManager;
    private RaceConfig raceConfig;

    private Queue<RaceCheckpoint> checkpoints;
    private RaceState state = RaceState.Start;
    private Stopwatch timer = new();

    public RaceEncounter(EncounterManager encounterManager, ClientboundEncounterStart start) {
        this.Type = EncounterType.Race;
        this.encounterManager = encounterManager;
        this.raceConfig = start.Race.Config;

        var pins = this.raceConfig.MapPins
            .Select(x => {
                var checkpoint = new GameObject("RaceCheckpoint");
                checkpoint.tag = RaceCheckpoint.Tag;
                checkpoint.transform.position = ((System.Numerics.Vector3) x).ToMentalDeficiency();
                var component = checkpoint.AddComponent<RaceCheckpoint>();
                return component;
            })
            .ToList();

        this.checkpoints = new Queue<RaceCheckpoint>();
        foreach (var pin in pins) this.checkpoints.Enqueue(pin);

        var player = WorldHandler.instance.GetCurrentPlayer();
        player.tf.position = ((System.Numerics.Vector3) this.raceConfig.StartPosition).ToMentalDeficiency();
        player.motor.SetVelocityTotal(
            Vector3.Zero.ToMentalDeficiency(),
            Vector3.Zero.ToMentalDeficiency(),
            Vector3.Zero.ToMentalDeficiency()
        );
        player.StopCurrentAbility();
        player.boostCharge = 0;

        this.encounterManager.InputBlocker.ShouldIgnoreInput = true;

        // Respawn all boost pickups
        UnityEngine.Object.FindObjectsOfType<Pickup>()
            .Where(pickup => pickup.pickupType is Pickup.PickUpType.BOOST_CHARGE or Pickup.PickUpType.BOOST_BIG_CHARGE)
            .ToList()
            .ForEach(boost => { boost.SetPickupActive(true); });

        this.timer.Start();
        this.IsBusy = true;
    }

    public override void Update() {
        var checkpoint = this.GetNextCheckpoint();
        if (checkpoint != null) checkpoint.UpdateUIIndicator();

        switch (this.state) {
            case RaceState.Start: {
                    if (this.timer.Elapsed.TotalSeconds > 3) {
                        this.state = RaceState.Race;
                        this.encounterManager.InputBlocker.ShouldIgnoreInput = false;
                        this.timer.Restart();
                    } else {
                        var secondsLeft = (int) Math.Ceiling(3 - this.timer.Elapsed.TotalSeconds);
                        var periods = new string('.', secondsLeft);
                        var countdown = secondsLeft.ToString(CultureInfo.CurrentCulture) + periods;
                        this.SetTimer(countdown);
                    }

                    var phone = WorldHandler.instance.GetCurrentPlayer().phone;
                    var app = phone.GetAppInstance<AppSlopCrew>();
                    break;
                }

            case RaceState.Race: {
                    this.SetTimer(this.TimeElapsed());
                    break;
                }
        }
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
        this.encounterManager.InputBlocker.ShouldIgnoreInput = false;
    }

    public override void HandleUpdate(ClientboundEncounterUpdate update) {
        this.UpdateRaceResults(update.Race.Times);
    }

    public override void HandleEnd(ClientboundEncounterEnd end) {
        this.UpdateRaceResults(end.Race.Times);
        this.Stop();
    }

    private void UpdateRaceResults(RepeatedField<RaceTime> times) {
        var player = WorldHandler.instance.GetCurrentPlayer();
        var app = player.phone.GetAppInstance<AppEncounters>();
        var sorted = times.OrderBy(x => x.Time).ToList();

        var str = string.Empty;
        var playerManager = Plugin.Host.Services.GetRequiredService<PlayerManager>();
        var config = Plugin.Host.Services.GetRequiredService<Config>();
        foreach (var kvp in sorted) {
            var id = kvp.PlayerId;
            var timeStr = "<color=white>: " + this.NiceTimerString(kvp.Time) + "\n";

            if (playerManager.Players.TryGetValue(id, out var associatedPlayer)) {
                var name = PlayerNameFilter.DoFilter(associatedPlayer.SlopPlayer.Name);
                str += name + timeStr;
            } else {
                // Assume players not in our players list is us
                str += config.General.Username.Value + timeStr;
            }
        }

        app.SetBigText("Race Results", str.Trim());
    }

    public bool OnCheckpointReached(RaceCheckpoint checkpoint) {
        var nextPin = this.checkpoints.Peek();
        if (nextPin.Pin != checkpoint.Pin) return false;

        this.checkpoints.Dequeue();

        this.encounterManager.ConnectionManager.SendMessage(new ServerboundMessage {
            EncounterUpdate = new ServerboundEncounterUpdate {
                Type = EncounterType.Race,
                Race = new ServerboundRaceEncounterUpdateData {
                    MapPin = this.raceConfig.MapPins.Count - this.checkpoints.Count
                }
            }
        });

        if (this.checkpoints.Count == 0) {
            this.state = RaceState.Finish;

            this.SetTimer(this.TimeElapsed());

            this.timer.Reset();
            this.timer.Stop();

            Core.Instance.AudioManager.PlaySfxUI(
                SfxCollectionID.EnvironmentSfx,
                AudioClipID.MascotUnlock
            );
        }

        return true;
    }

    private void SetTimer(string? text) {
        var gameplay = Core.Instance.UIManager.gameplay;

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

    private enum RaceState {
        Start,
        Race,
        Finish
    }
}
