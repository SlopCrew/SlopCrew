using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using Reptile;
using UnityEngine.UIElements;

namespace SlopCrew.Plugin.Patches;

/*
 * This one does a lot of things, let's break it down:
 *
 * - We need to determine what CharacterVisual belongs to us.
 *   - This is accomplished by getting the current character's CharacterVisual and checking it against the instance.
 * - We need to determine when an effect is changed, as it's set every frame.
 *   - This is accomplished by both a prefix and postfix.
 * - We need to not let the game overwrite our changes to other characters.
 *   - This is solved similarly to animations, with a field that is changed before/after a call.
 *
 * Messy, huh?
 */
[HarmonyPatch(typeof(CharacterVisual))]
public class CharacterVisualPatch {
    private static BoostpackEffectMode LastBoostpackEffectMode = BoostpackEffectMode.OFF;
    private static FrictionEffectMode LastFrictionEffectMode = FrictionEffectMode.OFF;
    private static bool LastSpraycan = false;
    private static bool LastPhone = false;

    [HarmonyPrefix]
    [HarmonyPatch("SetBoostpackEffect")]
    public static bool SetBoostpackEffect_Prefix(
        CharacterVisual __instance, BoostpackEffectMode set, float overrideScale = -1f
    ) {
        var characterVisual = GetMyCharacterVisual();
        
        if (__instance == characterVisual) {
            LastBoostpackEffectMode = characterVisual.boostpackEffectMode;
        } else if (GetAssociatedPlayer(__instance) is not null) {
            var playerManager = Plugin.Host.Services.GetRequiredService<PlayerManager>();
            return playerManager.SettingVisual;
        }

        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch("SetBoostpackEffect")]
    public static void SetBoostpackEffect_Postfix(
        CharacterVisual __instance, BoostpackEffectMode set, float overrideScale = -1f
    ) {
        var characterVisual = GetMyCharacterVisual();
        
        if (__instance == characterVisual) {
            var different = LastBoostpackEffectMode != set;
            if (different) {
                var localPlayerManager = Plugin.Host.Services.GetRequiredService<LocalPlayerManager>();
                localPlayerManager.VisualRefreshQueued = true;
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch("SetFrictionEffect")]
    public static bool SetFrictionEffect_Prefix(CharacterVisual __instance, FrictionEffectMode set) {
        var characterVisual = GetMyCharacterVisual();
        
        if (__instance == characterVisual) {
            LastFrictionEffectMode = characterVisual.frictionEffectMode;
        } else if (GetAssociatedPlayer(__instance) is not null) {
            var playerManager = Plugin.Host.Services.GetRequiredService<PlayerManager>();
            return playerManager.SettingVisual;
        }

        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch("SetFrictionEffect")]
    public static void SetFrictionEffect_Postfix(CharacterVisual __instance, FrictionEffectMode set) {
        var characterVisual = GetMyCharacterVisual();
        
        if (__instance == characterVisual) {
            var different = LastFrictionEffectMode != set;
            if (different) {
                var localPlayerManager = Plugin.Host.Services.GetRequiredService<LocalPlayerManager>();
                localPlayerManager.VisualRefreshQueued = true;
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch("SetSpraycan")]
    public static bool SetSpraycan_Prefix(CharacterVisual __instance, bool set, Characters c = Characters.NONE) {
        var characterVisual = GetMyCharacterVisual();
        
        if (__instance == characterVisual) {
            LastSpraycan = characterVisual.VFX.spraycan;
        } else if (GetAssociatedPlayer(__instance) is not null) {
            var playerManager = Plugin.Host.Services.GetRequiredService<PlayerManager>();
            return playerManager.SettingVisual;
        }

        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch("SetSpraycan")]
    public static void SetSpraycan_Postfix(CharacterVisual __instance, bool set, Characters c = Characters.NONE) {
        var characterVisual = GetMyCharacterVisual();
        
        if (__instance == characterVisual) {
            var different = LastSpraycan != set;
            if (different) {
                var localPlayerManager = Plugin.Host.Services.GetRequiredService<LocalPlayerManager>();
                localPlayerManager.VisualRefreshQueued = true;
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch("SetPhone")]
    public static bool SetPhone_Prefix(CharacterVisual __instance, bool set) {
        var characterVisual = GetMyCharacterVisual();
        
        if (__instance == characterVisual) {
            LastPhone = characterVisual.VFX.phone;
        } else if (GetAssociatedPlayer(__instance) is not null) {
            var playerManager = Plugin.Host.Services.GetRequiredService<PlayerManager>();
            return playerManager.SettingVisual;
        }

        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch("SetPhone")]
    public static void SetPhone_Postfix(CharacterVisual __instance, bool set) {
        var characterVisual = GetMyCharacterVisual();
        if (__instance == characterVisual) {
            var different = LastPhone != set;
            if (different) {
                var localPlayerManager = Plugin.Host.Services.GetRequiredService<LocalPlayerManager>();
                localPlayerManager.VisualRefreshQueued = true;
            }
        }
    }

    private static CharacterVisual GetMyCharacterVisual()
        => WorldHandler.instance.GetCurrentPlayer().characterVisual;

    private static AssociatedPlayer? GetAssociatedPlayer(CharacterVisual instance) {
        var playerManager = Plugin.Host.Services.GetRequiredService<PlayerManager>();
        var associatedPlayers = playerManager.AssociatedPlayers;

        foreach (var associatedPlayer in associatedPlayers) {
            var reptilePlayer = associatedPlayer.ReptilePlayer;
            if (reptilePlayer == null) continue;

            var characterVisual = reptilePlayer.characterVisual;
            if (characterVisual == instance) return associatedPlayer;
        }

        return null;
    }
}
