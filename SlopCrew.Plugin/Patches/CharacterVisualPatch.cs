﻿using HarmonyLib;
using Reptile;

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

    [HarmonyPrefix]
    [HarmonyPatch("SetBoostpackEffect")]
    public static bool SetBoostpackEffect_Prefix(
        CharacterVisual __instance, BoostpackEffectMode set, float overrideScale = -1f
    ) {
        var characterVisual = GetMyCharacterVisual();
        if (__instance == characterVisual) {
            LastBoostpackEffectMode = characterVisual.boostpackEffectMode;
        } else if (GetAssociatedPlayer(__instance) is not null) {
            return Plugin.PlayerManager.IsSettingVisual;
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
            if (different) Plugin.PlayerManager.IsVisualRefreshQueued = true;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch("SetFrictionEffect")]
    public static bool SetFrictionEffect_Prefix(CharacterVisual __instance, FrictionEffectMode set) {
        var characterVisual = GetMyCharacterVisual();
        if (__instance == characterVisual) {
            LastFrictionEffectMode = characterVisual.frictionEffectMode;
        } else if (GetAssociatedPlayer(__instance) is not null) {
            return Plugin.PlayerManager.IsSettingVisual;
        }

        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch("SetFrictionEffect")]
    public static void SetFrictionEffect_Postfix(CharacterVisual __instance, FrictionEffectMode set) {
        var characterVisual = GetMyCharacterVisual();
        if (__instance == characterVisual) {
            var different = LastFrictionEffectMode != set;
            if (different) Plugin.PlayerManager.IsVisualRefreshQueued = true;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch("SetSpraycan")]
    public static bool SetSpraycan_Prefix(CharacterVisual __instance, bool set, Characters c = Characters.NONE) {
        var characterVisual = GetMyCharacterVisual();
        if (__instance == characterVisual) {
            LastSpraycan = characterVisual.VFX.spraycan;
        } else if (GetAssociatedPlayer(__instance) is not null) {
            return Plugin.PlayerManager.IsSettingVisual;
        }

        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch("SetSpraycan")]
    public static void SetSpraycan_Postfix(CharacterVisual __instance, bool set, Characters c = Characters.NONE) {
        var characterVisual = GetMyCharacterVisual();
        if (__instance == characterVisual) {
            var different = LastSpraycan != set;
            if (different) Plugin.PlayerManager.IsVisualRefreshQueued = true;
        }
    }

    private static CharacterVisual? GetMyCharacterVisual() {
        var me = WorldHandler.instance?.GetCurrentPlayer();
        if (me is null) return null;

        var traverse = Traverse.Create(me);
        var characterVisual = traverse.Field<CharacterVisual>("characterVisual").Value;
        return characterVisual;
    }

    private static AssociatedPlayer? GetAssociatedPlayer(CharacterVisual instance) {
        var associatedPlayers = Plugin.PlayerManager.AssociatedPlayers;

        foreach (var associatedPlayer in associatedPlayers) {
            var reptilePlayer = associatedPlayer.ReptilePlayer;
            var traverse = Traverse.Create(reptilePlayer);
            var characterVisual = traverse.Field<CharacterVisual>("characterVisual").Value;
            if (characterVisual == instance) return associatedPlayer;
        }

        return null;
    }
}
