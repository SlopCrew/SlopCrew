using HarmonyLib;
using Reptile;
using Reptile.Phone;
using SlopCrew.Plugin.UI.Phone;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace SlopCrew.Plugin; 

// TODO: remove this class. This should not make it to prod. ~Sylvie
public class PhoneTests {
    class PleaseDontExplode : AUnlockable { }

    public static void StartTests() {
        Core.OnUpdate += Update;
    }

    private static void Update() {
        if (Input.GetKeyDown(KeyCode.O)) RingPhone();
    }

    public static void RingPhone() {
        var player = Reptile.WorldHandler.instance.GetCurrentPlayer();
        var phone = Traverse.Create(player).Field<Reptile.Phone.Phone>("phone").Value;
        var app = phone.GetAppInstance<AppSlopCrew>();
        var emailApp = phone.GetAppInstance<AppEmail>();
        var emailNotif = emailApp.GetComponent<Notification>();
        var phoneState = Traverse.Create(phone).Field<int>("state").Value;
        var showOnScreen = phoneState is 1 or 2; // on or booting up
        
        app.SetNotification(emailNotif);
        
        phone.PushNotification(app, "ring ring bitch", null);
        /*emailNotif.ShowNotification(phone,
                                    "ring ring bitch",
                                    showOnScreen);*/
    }
}
