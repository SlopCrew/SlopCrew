using HarmonyLib;
using Reptile;
using UnityEngine;

namespace SlopCrew.Plugin.Patches;

[HarmonyPatch(typeof(BMXOnlyGateway))]
public class BMXOnlyGatewayPatch {
    [HarmonyPrefix]
    [HarmonyPatch("OnTriggerStay")]
    private static bool OnTriggerStay(BMXOnlyGateway __instance, Collider other) {
        if (Plugin.SlopConfig.FixBikeGate.Value) {
            var plr = Traverse.Create(__instance).Field<Player>("p").Value;
            var associatedPlayer = Plugin.PlayerManager.GetAssociatedPlayer(plr);
            return associatedPlayer == null;
        }

        return true;
    }
}
