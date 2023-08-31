using System;
using System.Diagnostics;
using System.Globalization;
using HarmonyLib;
using Reptile;
using SlopCrew.Common;

namespace SlopCrew.Plugin;

public class SlopScoreEncounter {
    private const double StartDuration = 3;
    private const double PlayDuration = 90;
    private const double OutroDuration = 5;

    private float myScore;
    private float theirScore;
    private AssociatedPlayer? opponent;
    private CultureInfo cultureInfo = null!;

    private Stopwatch stopwatch = new();
    private State state = State.Stopped;

    private enum State {
        Stopped,
        Start,
        Play,
        Outro
    }

    public SlopScoreEncounter() {
        Core.OnUpdate += this.Update;
        StageManager.OnStagePostInitialization += this.Stop;
    }

    public void Start(uint encounterStartPlayerID) {
        this.cultureInfo = CultureInfo.CurrentCulture;

        if (Plugin.PlayerManager.Players.TryGetValue(encounterStartPlayerID, out var associatedPlayer)) {
            this.opponent = associatedPlayer;
            this.state = State.Start;
            this.stopwatch.Restart();
        }
    }

    public bool IsBusy() => this.state != State.Stopped;

    private void Stop() {
        var uiManager = Core.Instance.UIManager;
        var gameplay = Traverse.Create(uiManager).Field<GameplayUI>("gameplay").Value;

        gameplay.challengeGroup.SetActive(false);
        gameplay.timeLimitLabel.text = "";
        gameplay.targetScoreLabel.text = "";
        gameplay.totalScoreLabel.text = "";
        gameplay.targetScoreTitleLabel.text = "";
        gameplay.totalScoreTitleLabel.text = "";

        this.opponent = null;
        this.state = State.Stopped;
    }

    private void Update() {
        if (this.opponent is null || this.state == State.Stopped) return;
        if (!this.opponent.IsValid()) {
            this.Stop();
            return;
        }

        var elapsed = this.stopwatch.Elapsed.TotalSeconds;
        switch (this.state) {
            case State.Start: {
                if (this.EnsureElapsed(elapsed, State.Play)) {
                    // Clear their score when it's time to play
                    var player = WorldHandler.instance.GetCurrentPlayer();
                    var traverse = Traverse.Create(player);
                    traverse.Field<float>("score").Value = 0f;
                    traverse.Field<float>("baseScore").Value = 0f;
                    traverse.Field<float>("multiplier").Value = 1f;
                    return;
                }

                var secondsLeft = (int) Math.Ceiling(StartDuration - elapsed);
                var periods = new string('.', secondsLeft);
                var countdown = secondsLeft.ToString(this.cultureInfo) + periods;
                this.SetScoreUI(countdown);

                break;
            }

            case State.Play: {
                if (this.EnsureElapsed(elapsed, State.Outro)) return;

                var score = Plugin.PlayerManager.LastScoreAndMultiplier.Item1;
                var baseScore = Plugin.PlayerManager.LastScoreAndMultiplier.Item2;
                var scoreMultiplier = Plugin.PlayerManager.LastScoreAndMultiplier.Item3;

                var opponentScore = this.opponent.Score;
                var opponentBaseScore = this.opponent.BaseScore;
                var opponentScoreMultiplier = this.opponent.Multiplier;

                this.myScore = score + (baseScore * scoreMultiplier);
                this.theirScore = opponentScore + (opponentBaseScore * opponentScoreMultiplier);

                var remaining = PlayDuration - elapsed;
                if (remaining < 0) remaining = 0;
                this.SetScoreUI(this.NiceTimerString(remaining));

                break;
            }

            case State.Outro: {
                if (this.EnsureElapsed(elapsed, State.Stopped)) {
                    this.Stop();
                    return;
                }
                this.SetScoreUI("Finish!");
                break;
            }
        }
    }

    private bool EnsureElapsed(double elapsed, State nextState) {
        var required = this.state switch {
            State.Start => StartDuration,
            State.Play => PlayDuration,
            State.Outro => OutroDuration,
            _ => 0
        };

        if (elapsed >= required) {
            Plugin.Log.LogInfo($"State change: {this.state} -> {nextState}");
            this.state = nextState;
            this.stopwatch.Restart();
            return true;
        }

        return false;
    }

    // Stolen from Reptile code
    private string NiceTimerString(double timer) {
        var str = timer.ToString(this.cultureInfo);
        var startIndex = ((int) timer).ToString().Length + 3;
        if (str.Length > startIndex) str = str.Remove(startIndex);
        if (timer == 0.0) str = "0.00";
        return str;
    }

    private string FormatPlayerScore(float score) {
        if (this.state == State.Start) return string.Empty;
        return FormattingUtility.FormatPlayerScore(this.cultureInfo, score);
    }

    private void SetScoreUI(string time) {
        var uiManager = Core.Instance.UIManager;
        var gameplay = Traverse.Create(uiManager).Field<GameplayUI>("gameplay").Value;
        gameplay.challengeGroup.SetActive(true);

        var myScoreStr = this.FormatPlayerScore(this.myScore);
        var theirScoreStr = this.FormatPlayerScore(this.theirScore);
        gameplay.totalScoreLabel.text = myScoreStr;
        gameplay.targetScoreLabel.text = theirScoreStr;

        var myName = PlayerNameFilter.DoFilter(Plugin.SlopConfig.Username.Value);
        var theirName = PlayerNameFilter.DoFilter(this.opponent!.SlopPlayer.Name);
        gameplay.totalScoreTitleLabel.text = myName;
        gameplay.targetScoreTitleLabel.text = theirName;

        gameplay.timeLimitLabel.text = time;
    }
}
