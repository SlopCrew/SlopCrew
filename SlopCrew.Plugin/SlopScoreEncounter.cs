namespace SlopCrew.Plugin;

public class SlopScoreEncounter : SlopEncounter {
    public override void Start(uint encounterStartPlayerID) {
        this.PlayDuration = 90;
        base.Start(encounterStartPlayerID);
    }
    
    public override void EncounterUpdate() {
        var score = Plugin.PlayerManager.LastScoreAndMultiplier.Item1;
        var baseScore = Plugin.PlayerManager.LastScoreAndMultiplier.Item2;
        var scoreMultiplier = Plugin.PlayerManager.LastScoreAndMultiplier.Item3;

        var opponentScore = this.Opponent.Score;
        var opponentBaseScore = this.Opponent.BaseScore;
        var opponentScoreMultiplier = this.Opponent.Multiplier;

        this.MyScore = score + (baseScore * scoreMultiplier);
        this.TheirScore = opponentScore + (opponentBaseScore * opponentScoreMultiplier);
    }
}
