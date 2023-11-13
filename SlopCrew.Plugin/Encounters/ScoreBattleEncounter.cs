using SlopCrew.Common.Proto;

namespace SlopCrew.Plugin.Encounters;

public class ScoreBattleEncounter : SimpleEncounter {

    private Score? myScore;
    private Score? opponentScore;
    
    public ScoreBattleEncounter(EncounterManager encounterManager, ClientboundEncounterStart start) : base(encounterManager, start) { }
    
    public override void UpdatePlay() {
        this.MyTotalScore = this.myScore is null ? 0 : this.CalculateScore(this.myScore);
        this.TheirTotalScore = this.opponentScore is null ? 0 : this.CalculateScore(this.opponentScore);
    }

    private int CalculateScore(Score score) {
        return score.Score_ + (score.BaseScore * score.Multiplier);
    }

    public override void HandleUpdate(ClientboundEncounterUpdate update) {
        if (update.Type is EncounterType.ScoreBattle) {
            this.myScore = update.Simple.YourScore;
            this.opponentScore = update.Simple.OpponentScore;
        }
    }

    public override void HandleEnd(ClientboundEncounterEnd end) {
        if (end.Type is EncounterType.ScoreBattle) {
            this.myScore = end.Simple.YourScore;
            this.opponentScore = end.Simple.OpponentScore;
        }

        base.HandleEnd(end);
    }
}
