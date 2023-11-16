using SlopCrew.Common.Proto;

namespace SlopCrew.Plugin.Encounters;

public class ComboBattleEncounter : SimpleEncounter {
    private bool myComboDropped;
    private bool opponentComboDropped;
    private Score? myScore;
    private Score? opponentScore;

    public ComboBattleEncounter(EncounterManager encounterManager, ClientboundEncounterStart start) : base(
        encounterManager, start) {
        this.Type = EncounterType.ComboBattle;
    }

    public override void UpdatePlay() {
        const string comboDropped = "<b>Combo Dropped!</b>";
        if (this.myComboDropped) this.MyScoreMessage = comboDropped;
        if (this.opponentComboDropped) this.TheirScoreMessage = comboDropped;

        this.MyTotalScore = this.myScore is null ? 0 : this.CalculateScore(this.myScore);
        this.TheirTotalScore = this.opponentScore is null ? 0 : this.CalculateScore(this.opponentScore);
    }

    private int CalculateScore(Score score) {
        return score.BaseScore * score.Multiplier;
    }

    public override void Update() {
        if (this.State == TimerState.Outro) {
            this.MyScoreMessage = null;
            this.TheirScoreMessage = null;
        }

        base.Update();
    }

    public override void HandleUpdate(ClientboundEncounterUpdate update) {
        if (update.Type is EncounterType.ComboBattle) {
            this.myScore = update.Simple.YourScore;
            this.opponentScore = update.Simple.OpponentScore;
            this.myComboDropped = update.Simple.YourComboDropped;
            this.opponentComboDropped = update.Simple.OpponentComboDropped;
        }
    }

    public override void HandleEnd(ClientboundEncounterEnd end) {
        if (end.Type is EncounterType.ComboBattle) {
            this.myScore = end.Simple.YourScore;
            this.opponentScore = end.Simple.OpponentScore;
            base.HandleEnd(end);
        }
    }
}
