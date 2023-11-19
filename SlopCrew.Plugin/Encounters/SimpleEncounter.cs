using System;
using System.Diagnostics;
using System.Globalization;
using Reptile;
using SlopCrew.Common;
using SlopCrew.Common.Proto;

namespace SlopCrew.Plugin.Encounters;

public abstract class SimpleEncounter : Encounter {
    protected readonly EncounterManager encounterManager;
    protected double PlayDuration;

    protected float MyTotalScore;
    protected string? MyScoreMessage;
    protected float TheirTotalScore;
    protected string? TheirScoreMessage;
    protected AssociatedPlayer? Opponent;

    protected Stopwatch Stopwatch = new();
    public TimerState State = TimerState.Start;

    protected SimpleEncounter(
        EncounterManager encounterManager,
        ClientboundEncounterStart start
    ) {
        this.encounterManager = encounterManager;
        this.PlayDuration = start.Type switch {
            EncounterType.ScoreBattle => this.encounterManager!.ServerConfig.Hello!.ScoreBattleLength,
            EncounterType.ComboBattle => this.encounterManager!.ServerConfig.Hello!.ComboBattleLength,
            _ => 0
        };

        if (this.encounterManager.PlayerManager.Players.TryGetValue(start.Simple.PlayerId, out var opponent)) {
            var us = WorldHandler.instance.GetCurrentPlayer();
            if (us == null) return;
            us.AddBoostCharge(us.maxBoostCharge);

            this.Opponent = opponent;
            this.ResetPlayerScore();
            this.Stopwatch.Restart();

            this.IsBusy = true;
        }
    }

    private void ResetPlayerScore() {
        this.MyScoreMessage = null;
        this.TheirScoreMessage = null;

        var player = WorldHandler.instance.GetCurrentPlayer();
        if (player == null) return;

        player.score = 0;
        player.baseScore = 0;
        player.scoreMultiplier = 1;
    }

    public override void Update() {
        this.IsBusy = this.State != TimerState.Stopped;

        if (this.Opponent is null || this.State == TimerState.Stopped) return;
        if (this.Opponent.ReptilePlayer == null) {
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

                var secondsLeft = (int) Math.Ceiling(Constants.SimpleEncounterStartTime - elapsed);
                var periods = new string('.', secondsLeft);
                var countdown = secondsLeft + periods;
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

    public abstract void UpdatePlay();

    private bool EnsureElapsed(double elapsed, TimerState nextState) {
        var required = this.State switch {
            TimerState.Start => Constants.SimpleEncounterStartTime,
            TimerState.Play => PlayDuration,
            TimerState.Outro => Constants.SimpleEncounterEndTime,
            _ => 0
        };

        if (elapsed >= required) {
            this.SetEncounterState(nextState);
            return true;
        }

        return false;
    }

    protected virtual void SetEncounterState(TimerState nextState) {
        // Play a sound at the end of the battle
        if (nextState == TimerState.Outro) {
            Core.Instance.AudioManager.PlaySfxUI(
                SfxCollectionID.EnvironmentSfx,
                AudioClipID.MascotUnlock
            );
        }

        this.State = nextState;
        this.Stopwatch.Restart();
    }

    protected string FormatPlayerScore(float score) {
        return this.State == TimerState.Start
                   ? string.Empty
                   : FormattingUtility.FormatPlayerScore(CultureInfo.CurrentCulture, score);
    }

    private void SetScoreUI(string time) {
        var uiManager = Core.Instance.UIManager;
        if (uiManager == null) return;
        var gameplay = uiManager.gameplay;
        if (gameplay == null) return;

        gameplay.challengeGroup.SetActive(true);

        gameplay.totalScoreLabel.text =
            this.MyScoreMessage ?? this.FormatPlayerScore(this.MyTotalScore);
        gameplay.targetScoreLabel.text =
            this.TheirScoreMessage ?? this.FormatPlayerScore(this.TheirTotalScore);

        var username = this.encounterManager.Config.General.Username.Value;
        var myName = PlayerNameFilter.DoFilter(username);
        var theirName = PlayerNameFilter.DoFilter(this.Opponent!.SlopPlayer.Name);
        gameplay.totalScoreTitleLabel.text = myName;
        gameplay.targetScoreTitleLabel.text = theirName;

        gameplay.timeLimitLabel.text = time;
    }

    private void TurnOffScoreUI() {
        var uiManager = Core.Instance.UIManager;
        if (uiManager == null) return;
        var gameplay = uiManager.gameplay;
        if (gameplay == null) return;

        gameplay.challengeGroup.SetActive(false);
        gameplay.timeLimitLabel.text = "";
        gameplay.targetScoreLabel.text = "";
        gameplay.totalScoreLabel.text = "";
        gameplay.targetScoreTitleLabel.text = "";
        gameplay.totalScoreTitleLabel.text = "";
    }

    public override void HandleEnd(ClientboundEncounterEnd end) {
        this.SetEncounterState(TimerState.Outro);
    }

    public override void Stop() {
        base.Stop();
        this.TurnOffScoreUI();
        this.Opponent = null;
        this.State = TimerState.Stopped;
    }

    public enum TimerState {
        Stopped,
        Start,
        Play,
        Outro
    }
}
