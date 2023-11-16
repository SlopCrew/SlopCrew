using System;
using HarmonyLib;

namespace SlopCrew.Plugin.Patches;

[HarmonyPatch(typeof(Type))]
public class TypePatch {
    [HarmonyPostfix]
    [HarmonyPatch("GetType", typeof(string))]
    public static void GetType(string typeName, ref Type __result) {
        // Dear Team Reptile:
        // this.MyPhone.OpenApp(Type.GetType("Reptile.Phone." + appToOpen.AssignedApp.AppName))
        // What the fuck is wrong with you
        if (typeName == "Reptile.Phone.AppSlopCrew") {
            __result = typeof(UI.Phone.AppSlopCrew);
        }
    }
}
