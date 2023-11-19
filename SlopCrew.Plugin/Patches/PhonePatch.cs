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
        var appRoot = __instance.transform.Find("OpenCanvas/PhoneContainerOpen/MainScreen/Apps") as RectTransform;

        AppUtility.CreateApp<AppSlopCrew>("AppSlopCrew", appRoot!);
        AppUtility.CreateApp<AppQuickChat>("AppQuickChat", appRoot!);
        AppUtility.CreateApp<AppEncounters>("AppEncounters", appRoot!);
    }
}
