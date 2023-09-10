using HarmonyLib;
using Reptile;
using SlopCrew.Plugin.Scripts;
using UnityEngine;

namespace SlopCrew.Plugin.Patches {
    [HarmonyPatch(typeof(LedgeClimbAbility))]
    internal class LedgeClimbAbilityPatch {

        /// Prevent ledge climb from activating on checkpoints
        [HarmonyPrefix]
        [HarmonyPatch("CheckActivation")]
        private static bool CheckActivation(LedgeClimbAbility __instance) {
            var p = WorldHandler.instance.GetCurrentPlayer();
            float climbableHeightFromPos = Traverse.Create(__instance).Field("climbableHeightFromPos").GetValue<float>();

            //Decompiled way to check if we will hit a checkpoint
            int layerMask1 = 1;
            Vector3 origin = p.tf.position + p.motor.GetCapsule().radius * p.motor.dir * 1.3f + climbableHeightFromPos * Vector3.up;
            RaycastHit hitInfo1;
            if (Physics.Raycast(origin, Vector3.down, out hitInfo1, climbableHeightFromPos, layerMask1)) {
                if (hitInfo1.collider.gameObject.tag == SimpleCheckpoint.TAG) {
                    return false;
                }
            }
            return true;
        }
    }
}
