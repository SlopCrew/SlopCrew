using SlopCrew.Common.Encounters;

namespace SlopCrew.Plugin.Encounters;

public class SlopScoreEncounter : SlopTimerEncounter {
    public SlopScoreEncounter(SimpleEncounterConfigData configData) : base(configData) { }

    protected override void UpdatePlay() {
        var score = Plugin.PlayerManager.LastScoreAndMultiplier.Item1;
        var baseScore = Plugin.PlayerManager.LastScoreAndMultiplier.Item2;
        var scoreMultiplier = Plugin.PlayerManager.LastScoreAndMultiplier.Item3;

        var opponentScore = this.Opponent!.Score;
        var opponentBaseScore = this.Opponent.BaseScore;
        var opponentScoreMultiplier = this.Opponent.Multiplier;

        this.MyScore = score + (baseScore * scoreMultiplier);
        this.TheirScore = opponentScore + (opponentBaseScore * opponentScoreMultiplier);
    }
}
