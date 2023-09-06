using System;
using System.Diagnostics;
using System.Globalization;
using HarmonyLib;
using Reptile;
using SlopCrew.Common;
using SlopCrew.Common.Network.Clientbound;

namespace SlopCrew.Plugin.Encounters;

public class SlopEncounter {
    protected double PlayDuration;
    protected float MyScore;
    protected string? MyScoreMessage;
    protected float TheirScore;
    protected string? TheirScoreMessage;
    protected AssociatedPlayer? Opponent;

    private const double StartDuration = 3;
    private const double OutroDuration = 5;

    private CultureInfo cultureInfo = null!;

    protected Stopwatch Stopwatch = new();
    private SlopEncounterState slopEncounterState = SlopEncounterState.Stopped;

    public enum SlopEncounterState {
        Stopped,
        Start,
        Play,
        Outro
    }

    public SlopEncounter() {
        Core.OnUpdate += this.Update;
        StageManager.OnStagePostInitialization += this.Stop;
    }

    public virtual void Start(ClientboundEncounterStart encounterStart) {
        this.PlayDuration = encounterStart.EncounterConfig.PlayDuration;
        this.cultureInfo = CultureInfo.CurrentCulture;

        if (Plugin.PlayerManager.Players.TryGetValue(encounterStart.PlayerID, out var associatedPlayer)) {
            this.Opponent = associatedPlayer;
            this.slopEncounterState = SlopEncounterState.Start;
            this.ResetPlayerScore();
            this.Stopwatch.Restart();
        }
    }

    public bool IsBusy() => this.slopEncounterState != SlopEncounterState.Stopped;

    private void Stop() {
        this.TurnOffScoreUI();
        this.Opponent = null;
        this.slopEncounterState = SlopEncounterState.Stopped;
    }

    private void Update() {
        if (this.Opponent is null || this.slopEncounterState == SlopEncounterState.Stopped) return;
        if (!this.Opponent.IsValid()) {
            this.Stop();
            return;
        }
        if (this.Opponent.SlopPlayer.IsDead) {
            this.Stop();
            return;
        }

        var elapsed = this.Stopwatch.Elapsed.TotalSeconds;
        switch (this.slopEncounterState) {
            case SlopEncounterState.Start: {
                if (this.EnsureElapsed(elapsed, SlopEncounterState.Play)) {
                    this.ResetPlayerScore();
                    return;
                }

                var secondsLeft = (int) Math.Ceiling(StartDuration - elapsed);
                var periods = new string('.', secondsLeft);
                var countdown = secondsLeft.ToString(this.cultureInfo) + periods;
                this.SetScoreUI(countdown);

                break;
            }

            case SlopEncounterState.Play: {
                if (this.EnsureElapsed(elapsed, SlopEncounterState.Outro)) return;

                this.EncounterUpdate();

                var remaining = PlayDuration - elapsed;
                if (remaining < 0) remaining = 0;
                this.SetScoreUI(this.NiceTimerString(remaining));

                break;
            }

            case SlopEncounterState.Outro: {
                if (this.EnsureElapsed(elapsed, SlopEncounterState.Stopped)) {
                    this.Stop();
                    return;
                }
                this.SetScoreUI("Finish!");
                break;
            }
        }
    }

    private bool EnsureElapsed(double elapsed, SlopEncounterState nextState) {
        var required = this.slopEncounterState switch {
            SlopEncounterState.Start => StartDuration,
            SlopEncounterState.Play => PlayDuration,
            SlopEncounterState.Outro => OutroDuration,
            _ => 0
        };

        if (elapsed >= required) {
            this.SetEncounterState(nextState);
            return true;
        }

        return false;
    }

    public virtual void SetEncounterState(SlopEncounterState nextState) {
        Plugin.Log.LogInfo($"State change: {this.slopEncounterState} -> {nextState}");

        // Play a sound at the end of the battle
        if (nextState == SlopEncounterState.Outro) {
            var audioManager = Core.Instance.AudioManager;
            var playSfx = AccessTools.Method("Reptile.AudioManager:PlaySfxUI",
                                             new[] {typeof(SfxCollectionID), typeof(AudioClipID), typeof(float)});
            playSfx.Invoke(audioManager, new object[] {SfxCollectionID.EnvironmentSfx, AudioClipID.MascotUnlock, 0f});
        }

        this.slopEncounterState = nextState;
        this.Stopwatch.Restart();
    }

    public virtual void EncounterUpdate() { }

    // Stolen from Reptile code
    private string NiceTimerString(double timer) {
        var str = timer.ToString(this.cultureInfo);
        var startIndex = ((int) timer).ToString().Length + 3;
        if (str.Length > startIndex) str = str.Remove(startIndex);
        if (timer == 0.0) str = "0.00";
        return str;
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

    protected string FormatPlayerScore(float score) {
        if (this.slopEncounterState == SlopEncounterState.Start) return string.Empty;
        return FormattingUtility.FormatPlayerScore(this.cultureInfo, score);
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
}
