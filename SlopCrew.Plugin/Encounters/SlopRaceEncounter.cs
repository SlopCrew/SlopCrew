using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using HarmonyLib;
using Reptile;
using SlopCrew.Common.Encounters;
using SlopCrew.Common.Network.Serverbound;
using SlopCrew.Plugin.Encounters.Race;
using SlopCrew.Plugin.Extensions;
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
        this.GetNextCheckpoint()?.UpdateUIIndicator();

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
        foreach (var checkpointPin in this.checkpoints) {
            UnityEngine.Object.Destroy(checkpointPin.gameObject);
        }
        this.checkpoints.Clear();

        this.SetTimer(null);
        Plugin.ShouldIgnoreInput = false;
    }

    enum RaceState {
        Start,
        Race,
        Finish
    }
}
