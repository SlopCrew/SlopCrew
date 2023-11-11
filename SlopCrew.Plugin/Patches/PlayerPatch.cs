using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using Reptile;

namespace SlopCrew.Plugin.Patches;

[HarmonyPatch(typeof(Player))]
public class PlayerPatch {
    // Skip abilities on associated (networked) players
    // Attaching to grind rails causes the player position and VFX to rubberband on position updates
    [HarmonyPrefix]
    [HarmonyPatch("ActivateAbility")]
    public static bool ActivateAbility(Player __instance, Ability a) {
        if (__instance == WorldHandler.instance.GetCurrentPlayer()) {
            if (a is DieAbility) {
                var localPlayerManager = Plugin.Host.Services.GetRequiredService<LocalPlayerManager>();
                localPlayerManager.HelloRefreshQueued = true;
            }
            
            return true;
        }

        var playerManager = Plugin.Host.Services.GetRequiredService<PlayerManager>();
        var associatedPlayer = playerManager.GetAssociatedPlayer(__instance);
        return associatedPlayer == null;
    }
    
    [HarmonyPostfix]
    [HarmonyPatch("SetOutfit")]
    public static void SetOutfit(Player __instance, int setOutfit) {
        if (__instance == WorldHandler.instance.GetCurrentPlayer()) {
            var localPlayerManager = Plugin.Host.Services.GetRequiredService<LocalPlayerManager>();
            localPlayerManager.CurrentOutfit = setOutfit;
            localPlayerManager.HelloRefreshQueued = true;
        }
    }
}
