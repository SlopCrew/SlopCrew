using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using Reptile;
using UnityEngine;

namespace SlopCrew.Plugin.Patches;

[HarmonyPatch(typeof(PlayerPhoneCameras))]
public class PlayerPhoneCamerasPatch {
    [HarmonyPrefix]
    [HarmonyPatch("Awake")]
    public static bool Awake(PlayerPhoneCameras __instance) {
        // Loop up the parent chain to try and find our associated player
        var parent = __instance.gameObject.transform.parent;
        while (parent.gameObject.GetComponent<Player>() == null) {
            parent = parent.parent;
        }

        var playerManager = Plugin.Host.Services.GetRequiredService<PlayerManager>();
        var associatedPlayer = playerManager.GetAssociatedPlayer(parent.gameObject.GetComponent<Player>());
        if (associatedPlayer is not null) {
            // Also turn the cameras off for good measure
            var rear = __instance.transform.Find("rearCamera").GetComponent<Camera>();
            var front = __instance.transform.Find("frontCamera").GetComponent<Camera>();
            rear.enabled = false;
            front.enabled = false;
            return false;
        }

        return true;
    }
}
