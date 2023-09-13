using HarmonyLib;
using Reptile;

namespace SlopCrew.Plugin.Patches; 

[HarmonyPatch(typeof(PoliceCutscenes))]
public class PoliceCutscenesPatch {
    [HarmonyPrefix]
    [HarmonyPatch("PlaySequenceForStars")]
    public static bool PlaySequenceForStars(int stars) {
        // Skip cop cutscenes when you're in a battle
        return Plugin.CurrentEncounter?.IsBusy() != true;
    }
}
