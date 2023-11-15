using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using Reptile;
using SlopCrew.Plugin.Encounters;

namespace SlopCrew.Plugin.Patches;

[HarmonyPatch(typeof(PoliceCutscenes))]
public class PoliceCutscenesPatch {
    [HarmonyPrefix]
    [HarmonyPatch("PlaySequenceForStars")]
    public static bool PlaySequenceForStars(int stars) {
        // Skip cop cutscenes when you're in a battle
        var encounterManager = Plugin.Host.Services.GetRequiredService<EncounterManager>();
        return encounterManager.CurrentEncounter is not {IsBusy: true};
    }
}
