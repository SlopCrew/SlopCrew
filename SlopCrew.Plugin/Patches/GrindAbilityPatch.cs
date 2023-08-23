using HarmonyLib;
using Reptile;

namespace SlopCrew.Plugin.Patches;

[HarmonyPatch(typeof(GrindAbility))]
public class GrindAbilityPatch {
    // Even though we nop ActivateAbility, hopping onto a grind rail plays SFX/VFX
    // We nop this one too to make it not explode and spam the effects every frame
    [HarmonyPrefix]
    [HarmonyPatch("SetToLine")]
    public static bool SetToLine(GrindAbility __instance, GrindLine setGrindLine) {
        var associatedPlayer = Plugin.PlayerManager.GetAssociatedPlayer(__instance.p);
        return associatedPlayer == null;
    }
}
