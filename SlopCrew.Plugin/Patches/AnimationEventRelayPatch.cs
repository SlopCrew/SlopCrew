using HarmonyLib;
using Reptile;
using UnityEngine;

namespace SlopCrew.Plugin.Patches; 

// Should fix console errors relating to this class
// Todoish: loc_scuff exception in PlaySound
// Remind me when I have the time! -Sylvie
[HarmonyPatch(typeof(AnimationEventRelay))]
public class AnimationEventRelayPatch {
    [HarmonyPrefix]
    [HarmonyPatch("ActivateLeftLegCollider")]
    public static bool ActivateLeftLegCollider_Patch(AnimationEventRelay __instance) {
        var player = Traverse.Create(__instance).Field("player").GetValue<Player>();
        return player == WorldHandler.instance?.GetCurrentPlayer();
    }
    
    [HarmonyPrefix]
    [HarmonyPatch("ActivateRightLegCollider")]
    public static bool ActivateRightLegCollider_Patch(AnimationEventRelay __instance) {
        var player = Traverse.Create(__instance).Field("player").GetValue<Player>();
        return player == WorldHandler.instance?.GetCurrentPlayer();
    }
    
    [HarmonyPrefix]
    [HarmonyPatch("ActivateUpperBodyCollider")]
    public static bool ActivateUpperBodyCollider_Patch(AnimationEventRelay __instance) {
        var player = Traverse.Create(__instance).Field("player").GetValue<Player>();
        return player == WorldHandler.instance?.GetCurrentPlayer();
    }
    
    [HarmonyPrefix]
    [HarmonyPatch("DeactivateLeftLegCollider")]
    public static bool DeactivateLeftLegCollider_Patch(AnimationEventRelay __instance) {
        var player = Traverse.Create(__instance).Field("player").GetValue<Player>();
        return player == WorldHandler.instance?.GetCurrentPlayer();
    }
    
    [HarmonyPrefix]
    [HarmonyPatch("DeactivateRightLegCollider")]
    public static bool DeactivateRightLegCollider_Patch(AnimationEventRelay __instance) {
        var player = Traverse.Create(__instance).Field("player").GetValue<Player>();
        return player == WorldHandler.instance?.GetCurrentPlayer();
    }
    
    [HarmonyPrefix]
    [HarmonyPatch("DeactivateUpperBodyCollider")]
    public static bool DeactivateUpperBodyCollider_Patch(AnimationEventRelay __instance) {
        var player = Traverse.Create(__instance).Field("player").GetValue<Player>();
        return player == WorldHandler.instance?.GetCurrentPlayer();
    }

    // *Don't* only play if this.player == WorldHandler.instance?.GetCurrentPlayer()
    // because that makes it so that fake-player NPCs don't play sounds and that's not immersive ~Sylvie
    
    // This method isn't getting hit! Why??
    [HarmonyPrefix]
    [HarmonyPatch("PlaySound")]
    public static bool PlaySound_Patch(AnimationEventRelay __instance, AnimationEvent animationEvent) {
        var player = Traverse.Create(__instance).Field("player").GetValue<Player>();
        var style = Traverse.Create(player).Field("movestyleAudioCurrent").GetValue<MoveStyle>();
        Plugin.Log.LogDebug($"Player move style is '{style}'.");
        return true; // TODO: actually fix the bug
    }
}
