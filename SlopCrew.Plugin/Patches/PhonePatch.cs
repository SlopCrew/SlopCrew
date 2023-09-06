using HarmonyLib;
using Reptile;
using Reptile.Phone;

namespace SlopCrew.Plugin.Patches;

[HarmonyPatch(typeof(Phone))]
public class PhonePatch {
    [HarmonyPrefix]
    [HarmonyPatch("PhoneInit")]
    public static void PhoneInit(Phone __instance, Player setPlayer) {
        Plugin.PhoneInitializer.InitPhone(__instance.gameObject);
    }
}
