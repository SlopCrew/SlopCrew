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
    public TextMeshProUGUI? Label;
    private AssociatedPlayer? nearestPlayer;

    public override void Awake() {
        this.m_Unlockables = Array.Empty<AUnlockable>();
        base.Awake();
    }

    public override void OnPressRight() {
        if (this.nearestPlayer == null) return;

        Plugin.NetworkConnection.SendMessage(new ServerboundEncounterRequest {
            PlayerID = this.nearestPlayer.SlopPlayer.ID
        });
    }

    public override void OnAppUpdate() {
        var me = WorldHandler.instance.GetCurrentPlayer();
        if (me is null || this.Label is null) return;

        if (Plugin.SlopScoreEncounter.IsBusy()) {
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
            this.Label.text = "Press right\nto battle\n" +
                              PlayerNameFilter.DoFilter(this.nearestPlayer.SlopPlayer.Name);
        }
    }
}
