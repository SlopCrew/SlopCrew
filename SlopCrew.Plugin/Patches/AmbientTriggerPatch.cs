using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using Reptile;
using UnityEngine;

namespace SlopCrew.Plugin.Patches;

// Prevent AssociatedPlayers from fucking with the ambient lighting
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
        var config = Plugin.Host.Services.GetRequiredService<Config>();
        if (!config.Fixes.FixAmbientColors.Value) return null;

        var playerManager = Plugin.Host.Services.GetRequiredService<PlayerManager>();
        foreach (var player in playerManager.AssociatedPlayers) {
            var reptilePlayer = player.ReptilePlayer;
            if (reptilePlayer == null) continue;

            var collider = reptilePlayer.interactionCollider;
            if (collider == trigger) return player;
        }

        return null;
    }
}
