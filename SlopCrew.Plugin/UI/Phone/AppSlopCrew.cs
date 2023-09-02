using System;
using System.Linq;
using System.Numerics;
using Reptile;
using Reptile.Phone;
using SlopCrew.Common;
using SlopCrew.Common.Network.Serverbound;
using TMPro;

namespace SlopCrew.Plugin.UI.Phone;

public class AppSlopCrew : App {
    public TMP_Text? Label;
    private AssociatedPlayer? nearestPlayer;
    private EncounterType encounterType = EncounterType.ScoreEncounter;

    public override void Awake() {
        this.m_Unlockables = Array.Empty<AUnlockable>();
        base.Awake();
    }

    public override void OnPressUp() {
        this.encounterType = this.encounterType == EncounterType.ScoreEncounter
                                 ? EncounterType.ComboEncounter
                                 : EncounterType.ScoreEncounter;
    }

    public override void OnPressDown() {
        this.encounterType = this.encounterType == EncounterType.ScoreEncounter
                                 ? EncounterType.ComboEncounter
                                 : EncounterType.ScoreEncounter;
    }

    public override void OnPressRight() {
        if (this.nearestPlayer == null) return;

        Plugin.NetworkConnection.SendMessage(new ServerboundEncounterRequest {
            PlayerID = this.nearestPlayer.SlopPlayer.ID,
            EncounterType = this.encounterType
        });
    }

    public override void OnAppUpdate() {
        var me = WorldHandler.instance.GetCurrentPlayer();
        if (me is null || this.Label is null) return;

        if (Plugin.CurrentEncounter?.IsBusy() == true) {
            this.Label.text = "glhf";
            return;
        }

        var position = me.transform.position.FromMentalDeficiency();
        this.nearestPlayer = Plugin.PlayerManager.AssociatedPlayers
            .Where(x => x.IsValid())
            .OrderBy(x =>
                         Vector3.Distance(
                             x.ReptilePlayer.transform.position.FromMentalDeficiency(),
                             position
                         ))
            .FirstOrDefault();

        if (this.nearestPlayer == null) {
            this.Label.text = "No players nearby";
        } else {
            var modeName = this.encounterType switch {
                EncounterType.ScoreEncounter => "score",
                EncounterType.ComboEncounter => "combo"
            };
            this.Label.text = $"Press right\nto {modeName} battle\n" +
                              PlayerNameFilter.DoFilter(this.nearestPlayer.SlopPlayer.Name);
        }
    }
}
