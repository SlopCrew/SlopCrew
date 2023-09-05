using System;
using HarmonyLib;
using Reptile;
using SlopCrew.Common;

namespace SlopCrew.Plugin.Encounters;

public class SlopComboEncounter : SlopEncounter {
    private bool comboDropped = false;
    private bool opponentComboDropped = false;
    private float? lastComboScore;
    private float? opponentLastComboScore;

    public override void Start(uint encounterStartPlayerID, float encounterLength) {
        base.Start(encounterStartPlayerID, encounterLength);
        this.comboDropped = false;
        this.opponentComboDropped = false;
        this.lastComboScore = 0;
        this.opponentLastComboScore = 0;
    }

    public override void EncounterUpdate() {
        var elapsed = this.Stopwatch.Elapsed.TotalSeconds;

        if (elapsed > 15) {
            // If both players dropped their combo, end the encounter
            if (this.comboDropped && this.opponentComboDropped) {
                this.SetEncounterState(SlopEncounterState.Outro);
                return;
            }

            if (this.comboDropped)
                this.MyScoreMessage = "<b>Combo Dropped!</b>";
            if (this.opponentComboDropped)
                this.TheirScoreMessage = "<b>Combo Dropped!</b>";
        }

        if (!this.comboDropped) {
            var baseScore = Plugin.PlayerManager.LastScoreAndMultiplier.Item2;
            var multiplier = Plugin.PlayerManager.LastScoreAndMultiplier.Item3;

            if (elapsed > 15) {
                this.comboDropped = baseScore * multiplier < this.MyScore;
                this.lastComboScore = this.MyScore;
            }
            this.MyScore = baseScore * multiplier;
        }

        if (!this.opponentComboDropped) {
            var opponentBaseScore = this.Opponent.BaseScore;
            var opponentMultiplier = this.Opponent.Multiplier;

            if (elapsed > 15) {
                this.opponentComboDropped = opponentBaseScore * opponentMultiplier < this.TheirScore;
                this.opponentLastComboScore = this.TheirScore;
            }
            this.TheirScore = opponentBaseScore * opponentMultiplier;
        }
    }

    public override void SetEncounterState(SlopEncounterState nextState) {
        if (nextState == SlopEncounterState.Outro) {
            this.MyScoreMessage = this.FormatPlayerScore(this.lastComboScore ?? this.MyScore);
            this.TheirScoreMessage = this.FormatPlayerScore(this.opponentLastComboScore ?? this.TheirScore);
        }

        base.SetEncounterState(nextState);
    }
}
