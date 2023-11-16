using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using Reptile;
using UnityEngine;

namespace SlopCrew.Plugin.Patches;

[HarmonyPatch(typeof(BMXOnlyGateway))]
public class BMXOnlyGatewayPatch {
    [HarmonyPrefix]
    [HarmonyPatch("OnTriggerStay")]
    private static bool OnTriggerStay(BMXOnlyGateway __instance, Collider other) {
        var config = Plugin.Host.Services.GetRequiredService<Config>();
        
        if (config.Fixes.FixBikeGate.Value) {
            var playerManager = Plugin.Host.Services.GetRequiredService<PlayerManager>();
            var associatedPlayer = playerManager.GetAssociatedPlayer(__instance.p);
            return associatedPlayer == null;
        }

        return true;
    }
}
