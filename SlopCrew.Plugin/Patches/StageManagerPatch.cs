using HarmonyLib;
using Reptile;
using SlopCrew.Plugin.Encounters.Race;
using UnityEngine;

namespace SlopCrew.Plugin.Patches; 

[HarmonyPatch(typeof(StageManager))]
public class StageManagerPatch {
    [HarmonyPostfix]
    [HarmonyPatch("SetupStage")]
    private static void SetupStage() {
        Object.Instantiate(new GameObject("Racer")).AddComponent<RaceVelocityModifier>();
    }
}
