using HarmonyLib;
using Reptile;
using SlopCrew.Plugin.Encounters.Race;
using UnityEngine;

namespace SlopCrew.Plugin.Patches;

[HarmonyPatch(typeof(LedgeClimbAbility))]
internal class LedgeClimbAbilityPatch {
    // Prevent ledge climb from activating on checkpoints
    [HarmonyPrefix]
    [HarmonyPatch("CheckActivation")]
    private static bool CheckActivation(LedgeClimbAbility __instance) {
        var p = WorldHandler.instance.GetCurrentPlayer();
        var climbableHeightFromPos =
            Traverse.Create(__instance).Field("climbableHeightFromPos").GetValue<float>();

        // Decompiled way to check if we will hit a checkpoint
        const int layerMask1 = 1;
        var origin = p.tf.position
                     + (p.motor.GetCapsule().radius * p.motor.dir * 1.3f)
                     + (climbableHeightFromPos * Vector3.up);

        if (Physics.Raycast(origin, Vector3.down, out var hitInfo1, climbableHeightFromPos, layerMask1)) {
            if (hitInfo1.collider.gameObject.tag == RaceCheckpoint.Tag) {
                return false;
            }
        }

        return true;
    }
}
