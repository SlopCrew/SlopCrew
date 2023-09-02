namespace SlopCrew.Plugin;

public class SlopComboEncounter : SlopEncounter {
    public override void Start(uint encounterStartPlayerID) {
        this.PlayDuration = 300;
        base.Start(encounterStartPlayerID);
    }

    public override void EncounterUpdate() {
        var elapsed = this.Stopwatch.Elapsed.TotalSeconds;
        var baseScore = Plugin.PlayerManager.LastScoreAndMultiplier.Item2;
        var multiplier = Plugin.PlayerManager.LastScoreAndMultiplier.Item3;
        var opponentBaseScore = this.Opponent.BaseScore;
        var opponentMultiplier = this.Opponent.Multiplier;

        var failed = baseScore * multiplier < this.MyScore;
        var opponentFailed = opponentBaseScore * opponentMultiplier < this.TheirScore;
        if (elapsed > 10 && (failed || opponentFailed)) {
            this.SetEncounterState(SlopEncounterState.Outro);
            return;
        }

        this.MyScore = baseScore * multiplier;
        this.TheirScore = opponentBaseScore * opponentMultiplier;
    }
}
