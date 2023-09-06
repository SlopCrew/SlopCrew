using HarmonyLib;
using Reptile;
using SlopCrew.Plugin.Scripts;
using UnityEngine;

namespace SlopCrew.Plugin.Patches {

    [HarmonyPatch(typeof(StageManager))]
    public class StageManagerPatch {
        [HarmonyPostfix]
        [HarmonyPatch("SetupStage")]
        private static void SetupStage() {
            UnityEngine.Object.Instantiate<GameObject>(new GameObject("Racer")).AddComponent<RaceVelocityModifier>();
        }
    }
}
