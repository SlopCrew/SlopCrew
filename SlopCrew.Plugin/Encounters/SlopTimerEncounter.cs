using System;
using System.Diagnostics;
using System.Globalization;
using HarmonyLib;
using Reptile;
using SlopCrew.Common;
using SlopCrew.Common.Encounters;
using SlopCrew.Plugin.Extensions;

namespace SlopCrew.Plugin.Encounters;

public class SlopTimerEncounter : SlopEncounter {
    private CultureInfo cultureInfo;

    private const double StartDuration = 3;
    private const double OutroDuration = 5;
    protected double PlayDuration;

    protected float MyScore;
    protected string? MyScoreMessage;
    protected float TheirScore;
    protected string? TheirScoreMessage;
    protected AssociatedPlayer? Opponent;

    protected Stopwatch Stopwatch = new();
    public TimerState State = TimerState.Start;

    protected SlopTimerEncounter(SimpleEncounterConfigData configData) : base(configData) {
        this.cultureInfo = CultureInfo.CurrentCulture;
        this.PlayDuration = configData.EncounterLength;

        if (Plugin.PlayerManager.Players.TryGetValue(configData.Opponent, out var associatedPlayer)) {
            var us = WorldHandler.instance.GetCurrentPlayer()!;
            var maxBoost = Traverse.Create(us).Field<float>("maxBoostCharge").Value;
            us.AddBoostCharge(maxBoost);

            this.Opponent = associatedPlayer;
            this.ResetPlayerScore();
            this.Stopwatch.Restart();
        }
    }

    protected override void Update() {
        this.IsBusy = this.State != TimerState.Stopped;

        if (this.Opponent is null || this.State == TimerState.Stopped) return;
        if (!this.Opponent.IsValid()) {
            this.Stop();
            return;
        }
        if (this.Opponent.SlopPlayer.IsDead) {
            this.Stop();
            return;
        }

        var elapsed = this.Stopwatch.Elapsed.TotalSeconds;
        switch (this.State) {
            case TimerState.Start: {
                if (this.EnsureElapsed(elapsed, TimerState.Play)) {
                    this.ResetPlayerScore();
                    return;
                }

                var secondsLeft = (int) Math.Ceiling(StartDuration - elapsed);
                var periods = new string('.', secondsLeft);
                var countdown = secondsLeft.ToString(this.cultureInfo) + periods;
                this.SetScoreUI(countdown);

                break;
            }

            case TimerState.Play: {
                if (this.EnsureElapsed(elapsed, TimerState.Outro)) return;

                this.UpdatePlay();

                var remaining = PlayDuration - elapsed;
                if (remaining < 0) remaining = 0;
                this.SetScoreUI(this.NiceTimerString(remaining));

                break;
            }

            case TimerState.Outro: {
                if (this.EnsureElapsed(elapsed, TimerState.Stopped)) {
                    this.Stop();
                    return;
                }
                this.SetScoreUI("Finish!");
                break;
            }
        }
    }

    protected virtual void UpdatePlay() { }

    protected override void Stop() {
        base.Stop();
        this.TurnOffScoreUI();
        this.Opponent = null;
        this.State = TimerState.Stopped;
    }

    private void ResetPlayerScore() {
        var player = WorldHandler.instance.GetCurrentPlayer();
        var traverse = Traverse.Create(player);
        traverse.Field<float>("score").Value = 0f;
        traverse.Field<float>("baseScore").Value = 0f;
        traverse.Field<float>("scoreMultiplier").Value = 1f;
        this.MyScoreMessage = null;
        this.TheirScoreMessage = null;
    }

    protected virtual void SetEncounterState(TimerState nextState) {
        Plugin.Log.LogInfo($"State change: {this.State} -> {nextState}");

        // Play a sound at the end of the battle
        if (nextState == TimerState.Outro) {
            Core.Instance.AudioManager.PlaySfx(
                SfxCollectionID.EnvironmentSfx,
                AudioClipID.MascotUnlock
            );
        }

        this.State = nextState;
        this.Stopwatch.Restart();
    }

    private bool EnsureElapsed(double elapsed, TimerState nextState) {
        var required = this.State switch {
            TimerState.Start => StartDuration,
            TimerState.Play => PlayDuration,
            TimerState.Outro => OutroDuration,
            _ => 0
        };

        if (elapsed >= required) {
            this.SetEncounterState(nextState);
            return true;
        }

        return false;
    }

    protected string FormatPlayerScore(float score) {
        return this.State == TimerState.Start
                   ? string.Empty
                   : FormattingUtility.FormatPlayerScore(this.cultureInfo, score);
    }

    private void SetScoreUI(string time) {
        var uiManager = Core.Instance.UIManager;
        var gameplay = Traverse.Create(uiManager).Field<GameplayUI>("gameplay").Value;
        gameplay.challengeGroup.SetActive(true);

        gameplay.totalScoreLabel.text =
            this.MyScoreMessage ?? this.FormatPlayerScore(this.MyScore);
        gameplay.targetScoreLabel.text =
            this.TheirScoreMessage ?? this.FormatPlayerScore(this.TheirScore);

        var myName = PlayerNameFilter.DoFilter(Plugin.SlopConfig.Username.Value);
        var theirName = PlayerNameFilter.DoFilter(this.Opponent!.SlopPlayer.Name);
        gameplay.totalScoreTitleLabel.text = myName;
        gameplay.targetScoreTitleLabel.text = theirName;

        gameplay.timeLimitLabel.text = time;
    }

    private void TurnOffScoreUI() {
        var uiManager = Core.Instance.UIManager;
        var gameplay = Traverse.Create(uiManager).Field<GameplayUI>("gameplay").Value;

        gameplay.challengeGroup.SetActive(false);
        gameplay.timeLimitLabel.text = "";
        gameplay.targetScoreLabel.text = "";
        gameplay.totalScoreLabel.text = "";
        gameplay.targetScoreTitleLabel.text = "";
        gameplay.totalScoreTitleLabel.text = "";
    }

    public enum TimerState {
        Stopped,
        Start,
        Play,
        Outro
    }
}
