using HarmonyLib;
using Reptile;
using Reptile.Phone;
using SlopCrew.Common;
using SlopCrew.Common.Network.Clientbound;
using TMPro;
using UnityEngine;
using Player = Reptile.Player;

namespace SlopCrew.Plugin.UI.Phone;

public class PhoneInitializer {
    public ClientboundEncounterRequest? LastRequest;

    // We need to shove our own GameObject with a AppSlopCrew component into the prefab
    public void InitPhone(GameObject phone) {
        var apps = phone.transform.Find("OpenCanvas/PhoneContainerOpen/MainScreen/Apps");

        var slopAppObj = new GameObject("AppSlopCrew");
        slopAppObj.layer = Layers.Phone;

        slopAppObj.AddComponent<AppSlopCrew>();
        slopAppObj.transform.SetParent(apps, false);
        // why are these zero? idk!
        slopAppObj.transform.localScale = new(1, 1, 1);
        slopAppObj.SetActive(true);
    }

    public void ShowNotif(ClientboundEncounterRequest request) {
        if (!Plugin.SlopConfig.ReceiveNotifications.Value) return;
        if (Plugin.CurrentEncounter?.IsBusy == true) return;

        if (Plugin.PlayerManager.Players.TryGetValue(request.PlayerID, out var associatedPlayer)) {
            var name = PlayerNameFilter.DoFilter(associatedPlayer.SlopPlayer.Name);

            var player = WorldHandler.instance.GetCurrentPlayer();
            var phone = Traverse.Create(player).Field<Reptile.Phone.Phone>("phone").Value;
            var app = phone.GetAppInstance<AppSlopCrew>();

            var emailApp = phone.GetAppInstance<AppEmail>();
            var emailNotif = emailApp.GetComponent<Notification>();
            app.SetNotification(emailNotif);

            this.LastRequest = request;
            phone.PushNotification(app, name, null);
        }
    }
}
