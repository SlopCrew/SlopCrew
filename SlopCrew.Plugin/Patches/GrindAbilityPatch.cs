using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using Reptile;

namespace SlopCrew.Plugin.Patches;

[HarmonyPatch(typeof(GrindAbility))]
public class GrindAbilityPatch {
    // Even though we nop ActivateAbility, hopping onto a grind rail plays SFX/VFX
    // We nop this one too to make it not explode and spam the effects every frame
    [HarmonyPrefix]
    [HarmonyPatch("SetToLine")]
    public static bool SetToLine(GrindAbility __instance, GrindLine setGrindLine) {
        var playerManager = Plugin.Host.Services.GetRequiredService<PlayerManager>();
        var associatedPlayer = playerManager.GetAssociatedPlayer(__instance.p);
        return associatedPlayer == null;
    }
}
