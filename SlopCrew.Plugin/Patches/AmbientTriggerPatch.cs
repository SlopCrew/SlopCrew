using HarmonyLib;
using Reptile;
using UnityEngine;

namespace SlopCrew.Plugin.Patches;

[HarmonyPatch(typeof(AmbientTrigger))]
public class AmbientTriggerPatch {
    [HarmonyPrefix]
    [HarmonyPatch("OnTriggerEnter")]
    private static bool OnTriggerEnter(Collider trigger) {
        var associatedPlayer = GetAssociatedPlayer(trigger);
        return associatedPlayer == null;
    }

    [HarmonyPrefix]
    [HarmonyPatch("OnTriggerExit")]
    private static bool OnTriggerExit(Collider trigger) {
        var associatedPlayer = GetAssociatedPlayer(trigger);
        return associatedPlayer == null;
    }

    private static AssociatedPlayer? GetAssociatedPlayer(Collider trigger) {
        if (!Plugin.SlopConfig.FixAmbientColors.Value) return null;

        foreach (var player in Plugin.PlayerManager.AssociatedPlayers) {
            var collider = player.ReptilePlayer.interactionCollider;
            if (collider == trigger) return player;
        }

        return null;
    }
}
