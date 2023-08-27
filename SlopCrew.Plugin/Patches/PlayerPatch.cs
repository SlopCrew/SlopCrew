using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Reptile;
using SlopCrew.Common;
using SlopCrew.Common.Network.Clientbound;
using UnityEngine;
using Logger = UnityEngine.Logger;
using Player = Reptile.Player;

namespace SlopCrew.Plugin.Patches;

[HarmonyPatch(typeof(Player))]
public class PlayerPatch {
    // Skip abilities on associated (networked) players
    // Attaching to grind rails causes the player position and VFX to rubberband on position updates
    [HarmonyPrefix]
    [HarmonyPatch("ActivateAbility")]
    public static bool ActivateAbility(Player __instance, Ability a) {
        var associatedPlayer = Plugin.PlayerManager.GetAssociatedPlayer(__instance);
        return associatedPlayer == null;
    }

    [HarmonyPrefix]
    [HarmonyPatch("CheckWallrun")]
    private static bool CheckWallrun(Player __instance, Collision other) {
        var associatedPlayer = Plugin.PlayerManager.GetAssociatedPlayer(__instance);
        return associatedPlayer == null;
    }

    [HarmonyPrefix]
    [HarmonyPatch("PlayAnim")]
    public static bool PlayAnim(
        Player __instance, int newAnim, bool forceOverwrite = false, bool instant = false, float atTime = -1f
    ) {
        if (__instance == WorldHandler.instance?.GetCurrentPlayer()) {
            Plugin.PlayerManager.PlayAnimation(newAnim, forceOverwrite, instant, atTime);
            return true;
        } else if (Plugin.PlayerManager.GetAssociatedPlayer(__instance) is not null) {
            // Only let the animation play if it's us
            return Plugin.PlayerManager.IsPlayingAnimation;
        }

        return true;
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
        if (__instance == WorldHandler.instance?.GetCurrentPlayer()) {
            Plugin.PlayerManager.IsHelloRefreshQueued = true;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch("SetCharacter")]
    public static void SetCharacter(Player __instance, Characters setChar, int setOutfit = 0) {
        if (__instance == WorldHandler.instance?.GetCurrentPlayer()) {
            Plugin.PlayerManager.IsHelloRefreshQueued = true;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch("SetOutfit")]
    public static void SetOutfit(Player __instance, int setOutfit) {
        if (__instance == WorldHandler.instance?.GetCurrentPlayer()) {
            Plugin.PlayerManager.CurrentOutfit = setOutfit;
            Plugin.PlayerManager.IsHelloRefreshQueued = true;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch("FixedUpdatePlayer")]
    public static void FixedUpdatePlayer(Player __instance) {
        var associatedPlayer = Plugin.PlayerManager.GetAssociatedPlayer(__instance);

        if (associatedPlayer is not null) {
            associatedPlayer.timeElapsed += Time.deltaTime;
            Vector3 CurrentPosition = associatedPlayer.ReptilePlayer.motor.BodyPosition();

            while (associatedPlayer.positionUpdates.Count > 0) {
                var positionUpdate = associatedPlayer.positionUpdates.Dequeue();  // Peek at the next update without removing it

                if (PlayerManager.ServerTick < positionUpdate.Tick) {
                    // Not ready to process this update yet
                    break;
                }

                // Process the update
                associatedPlayer.prevPosition = associatedPlayer.targetPosition;
                associatedPlayer.targetPosition = positionUpdate;

                associatedPlayer.timeElapsed = 0f;
                associatedPlayer.timeToTarget = (associatedPlayer.targetPosition.Tick - associatedPlayer.prevPosition.Tick) *
                               Constants.TickRate;

                //associatedPlayer.positionUpdates.Dequeue();  // Remove the processed update
            }

            if (associatedPlayer.timeToTarget == 0f) {
                associatedPlayer.lerpAmount = 1f; // Instantly set to target if there's no time difference.
            } else {
                associatedPlayer.lerpAmount = associatedPlayer.timeElapsed / associatedPlayer.timeToTarget;
            }

            associatedPlayer.InterpolatePosition();
            associatedPlayer.InterpolateRotation();

            associatedPlayer.MapPin?.SetLocation();
        }
    }

    // Quarterpipe fix
    [HarmonyPrefix]
    [HarmonyPatch("CheckVert")]
    private static bool CheckVert(Player __instance, ref bool __result) {
        var associatedPlayer = Plugin.PlayerManager.GetAssociatedPlayer(__instance);
        if (associatedPlayer is not null) {
            __result = false;
            return false;
        }

        return true;
    }
}
