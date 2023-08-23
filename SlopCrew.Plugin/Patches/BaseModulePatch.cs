using HarmonyLib;
using Reptile;

namespace SlopCrew.Plugin.Patches;

[HarmonyPatch(typeof(BaseModule))]
public class BaseModulePatch {
    [HarmonyPostfix]
    [HarmonyPatch("HandleStageFullyLoaded")]
    private static void HandleStageFullyLoaded(BaseModule __instance) {
        //Plugin.PlayerManager.SpawnTest();
    }
}
