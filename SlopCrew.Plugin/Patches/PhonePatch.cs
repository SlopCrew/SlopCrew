using HarmonyLib;
using Reptile;
using Reptile.Phone;
using SlopCrew.Plugin.UI.Phone;
using UnityEngine;

namespace SlopCrew.Plugin.Patches;

[HarmonyPatch(typeof(Phone))]
public class PhonePatch {
    [HarmonyPrefix]
    [HarmonyPatch("PhoneInit")]
    public static void PhoneInit(Phone __instance, Player setPlayer) {
        var apps = __instance.transform.Find("OpenCanvas/PhoneContainerOpen/MainScreen/Apps");

        var slopAppObj = new GameObject("AppSlopCrew");
        slopAppObj.layer = Layers.Phone;

        slopAppObj.AddComponent<AppSlopCrew>();
        slopAppObj.transform.SetParent(apps, false);

        // why are these zero? idk!
        slopAppObj.transform.localScale = new(1, 1, 1);
        slopAppObj.SetActive(true);
    }
}
