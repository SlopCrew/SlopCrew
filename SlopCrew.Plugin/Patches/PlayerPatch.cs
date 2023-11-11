using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using Reptile;
using UnityEngine;

namespace SlopCrew.Plugin.Patches;

[HarmonyPatch(typeof(Player))]
public class PlayerPatch {
    // Skip abilities on AssociatedPlayers
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

    // Don't let AssociatedPlayers wallrun or it'll snap to them (same as above)
    [HarmonyPrefix]
    [HarmonyPatch("CheckWallrun")]
    private static bool CheckWallrun(Player __instance, Collision other) {
        var playerManager = Plugin.Host.Services.GetRequiredService<PlayerManager>();
        var associatedPlayer = playerManager.GetAssociatedPlayer(__instance);
        return associatedPlayer == null;
    }

    // Same as above but for quarterpipes
    [HarmonyPrefix]
    [HarmonyPatch("CheckVert")]
    private static bool CheckVert(Player __instance, ref bool __result) {
        var playerManager = Plugin.Host.Services.GetRequiredService<PlayerManager>();
        var associatedPlayer = playerManager.GetAssociatedPlayer(__instance);
        if (associatedPlayer is not null) {
            __result = false;
            return false;
        }

        return true;
    }

    // Don't let AssociatedPlayers interact with world triggers
    [HarmonyPrefix]
    [HarmonyPatch("OnTriggerStay")]
    public static bool OnTriggerStay(Player __instance, Collider other) {
        var playerManager = Plugin.Host.Services.GetRequiredService<PlayerManager>();
        var associatedPlayer = playerManager.GetAssociatedPlayer(__instance);
        return associatedPlayer == null;
    }

    [HarmonyPostfix]
    [HarmonyPatch("UpdateHoldProps")]
    private static void UpdateHoldProps(Player __instance) {
        var playerManager = Plugin.Host.Services.GetRequiredService<PlayerManager>();
        var associatedPlayer = playerManager.GetAssociatedPlayer(__instance);

        if (associatedPlayer is not null) {
            // this is basically copy pasted from a decompile, lol, lmao, etc
            var dt = Core.dt;

            if (associatedPlayer.PhoneOut) {
                __instance.phoneLayerWeight += __instance.grabPhoneSpeed * dt;
                __instance.characterVisual.SetPhone(true);
                if (__instance.phoneLayerWeight >= 1.0f) __instance.phoneLayerWeight = 1f;
                __instance.anim.SetLayerWeight(3, __instance.phoneLayerWeight);
            } else {
                __instance.phoneLayerWeight -= __instance.grabPhoneSpeed * dt;

                if (__instance.phoneLayerWeight <= 0.0f) {
                    __instance.phoneLayerWeight = 0.0f;
                    __instance.characterVisual.SetPhone(false);
                }

                __instance.anim.SetLayerWeight(3, __instance.phoneLayerWeight);
            }
        }
    }


    [HarmonyPostfix]
    [HarmonyPatch("UpdatePlayer")]
    public static void UpdatePlayer(Player __instance) {
        var playerManager = Plugin.Host.Services.GetRequiredService<PlayerManager>();
        var associatedPlayer = playerManager.GetAssociatedPlayer(__instance);
        associatedPlayer?.Update();
    }

    [HarmonyPostfix]
    [HarmonyPatch("SetMoveStyle")]
    protected static void SetMoveStyle(
        Player __instance,
        MoveStyle setMoveStyle,
        bool changeProp = true,
        bool changeAnim = true,
        GameObject specialSkateboard = null
    ) {
        if (__instance == WorldHandler.instance.GetCurrentPlayer()) {
            var localPlayerManager = Plugin.Host.Services.GetRequiredService<LocalPlayerManager>();
            localPlayerManager.HelloRefreshQueued = true;
        }
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

    [HarmonyPostfix]
    [HarmonyPatch("SetCharacter")]
    public static void SetCharacter(Player __instance, Characters setChar, int setOutfit = 0) {
        if (__instance == WorldHandler.instance.GetCurrentPlayer()) {
            // Since the game sets outfit after calling SetCharacter in CharacterSelect.SetPlayerToCharacter by setting
            // outfit material manually (instead of just using outfit parameter), we also have to set it manually
            var outfit = Core.Instance.SaveManager.CurrentSaveSlot.GetCharacterProgress(setChar)?.outfit;
            var localPlayerManager = Plugin.Host.Services.GetRequiredService<LocalPlayerManager>();
            if (outfit != null) localPlayerManager.CurrentOutfit = (int) outfit;
            localPlayerManager.HelloRefreshQueued = true;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch("SetSpraycanState")]
    private static bool SetSpraycanState(Player __instance, Player.SpraycanState state) {
        var playerManager = Plugin.Host.Services.GetRequiredService<PlayerManager>();
        var associatedPlayer = playerManager.GetAssociatedPlayer(__instance);

        if (__instance == WorldHandler.instance.GetCurrentPlayer()) {
            var localPlayerManager = Plugin.Host.Services.GetRequiredService<LocalPlayerManager>();
            localPlayerManager.HelloRefreshQueued = true;
        }

        // Only let us set spraycan state, not the game
        if (associatedPlayer is not null) return playerManager.SettingVisual;

        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch("PlayAnim")]
    public static bool PlayAnim(
        Player __instance, int newAnim, bool forceOverwrite = false, bool instant = false, float atTime = -1f
    ) {
        var playerManager = Plugin.Host.Services.GetRequiredService<PlayerManager>();

        if (__instance == WorldHandler.instance.GetCurrentPlayer()) {
            var localPlayerManager = Plugin.Host.Services.GetRequiredService<LocalPlayerManager>();
            localPlayerManager.PlayAnim(newAnim, forceOverwrite, instant, atTime);
            return true;
        }

        // Only let us play animations, not the game
        if (playerManager.GetAssociatedPlayer(__instance) is not null) return playerManager.PlayingAnimation;

        return true;
    }
}
