using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Reptile;
using Reptile.Phone;
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
        if (__instance == WorldHandler.instance?.GetCurrentPlayer()) {
            if (a is DieAbility) Plugin.PlayerManager.IsHelloRefreshQueued = true;
            return true;
        }

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

        if (associatedPlayer is null) {
            float num = float.MaxValue;
            SlopGraffitiSpot? graffitiSpot1 = null;
            foreach (SlopGraffitiSpot graffitiSpot2 in Plugin.SlopGraffitiSpotsAvailable)
            {
                if (graffitiSpot2 != null)
                {
                    float magnitude = (__instance.transform.position - graffitiSpot2.transform.position).magnitude;

                    if (magnitude < num)
                    {
                        num = magnitude;
                        graffitiSpot1 = graffitiSpot2;
                    }
                }
            }
            if (graffitiSpot1 is null) return;
            if (__instance.preSprayButtonHeld && !__instance.sprayButtonHeld || __instance.sprayButtonNew) {
                __instance.StartGraffitiMode(graffitiSpot1.OriginalGraffitiSpot);
                Plugin.TargetedGraffitiSpot = graffitiSpot1;
            }
            Plugin.SlopGraffitiSpotsAvailable.Clear();
        }
    }
    
    [HarmonyPostfix]
    [HarmonyPatch("UpdatePlayer")]
    public static void UpdatePlayer(Player __instance) {
        var associatedPlayer = Plugin.PlayerManager.GetAssociatedPlayer(__instance);

        if (associatedPlayer is not null) {
            associatedPlayer.TimeElapsed += Time.deltaTime;

            while (associatedPlayer.TransformUpdates.Count > 0) {
                var transformUpdate = associatedPlayer.TransformUpdates.Dequeue();

                if (Plugin.NetworkConnection.ServerTick > transformUpdate.Tick) {
                    associatedPlayer.TimeElapsed = 0f;

                    // Update target and previous target transform
                    associatedPlayer.PrevTarget = associatedPlayer.TargetTransform;
                    associatedPlayer.TargetTransform = transformUpdate;

                    // Calculate time to next target position
                    var lerpTime = (associatedPlayer.TargetTransform.Tick - associatedPlayer.PrevTarget.Tick) *
                                   Constants.TickRate;
                    var latency = (associatedPlayer.TargetTransform.Latency + Plugin.NetworkConnection.ServerLatency) /
                                  1000f / 2f;
                    associatedPlayer.TimeToTarget = lerpTime + latency;
                }
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

    // Don't let AssociatedPlayers interact with world triggers
    [HarmonyPrefix]
    [HarmonyPatch("OnTriggerStay")]
    public static bool OnTriggerStay(Player __instance, Collider other) {
        var associatedPlayer = Plugin.PlayerManager.GetAssociatedPlayer(__instance);
        if (associatedPlayer is null) {
            var slopGraffitiSpot = other.gameObject.GetComponentInParent<SlopGraffitiSpot>();

            if (other.gameObject.CompareTag("GraffitiSpot") && slopGraffitiSpot is not null) {
                var uiManager = Core.Instance.UIManager;
                var gameplay = Traverse.Create(uiManager).Field<GameplayUI>("gameplay").Value;
                var player = Traverse.Create(__instance);
                
                player.Field("graffitiContextAvailable").SetValue(10);
                gameplay.ShowContextLabelUI();
                Plugin.SlopGraffitiSpotsAvailable.Add(slopGraffitiSpot);
                return false;
            }
        }
        return associatedPlayer == null;
    }

    [HarmonyPostfix]
    [HarmonyPatch("UpdateHoldProps")]
    private static void UpdateHoldProps(Player __instance) {
        var associatedPlayer = Plugin.PlayerManager.GetAssociatedPlayer(__instance);

        if (associatedPlayer is not null) {
            var trPlr = Traverse.Create(__instance);
            var phoneLayerWeight = trPlr.Field<float>("phoneLayerWeight");
            var characterVisual = trPlr.Field<CharacterVisual>("characterVisual").Value;
            var anim = trPlr.Field<Animator>("anim").Value;

            // __instance is basically copy pasted from a decompile, lol, lmao, etc
            var dt = Core.dt;

            if (associatedPlayer.PhoneOut) {
                phoneLayerWeight.Value += __instance.grabPhoneSpeed * dt;
                characterVisual.SetPhone(true);
                if (phoneLayerWeight.Value >= 1.0f)
                    phoneLayerWeight.Value = 1f;
                anim.SetLayerWeight(3, phoneLayerWeight.Value);
            } else {
                phoneLayerWeight.Value -= __instance.grabPhoneSpeed * dt;
                if (phoneLayerWeight.Value <= 0.0f) {
                    phoneLayerWeight.Value = 0.0f;
                    characterVisual.SetPhone(false);
                }
                anim.SetLayerWeight(3, phoneLayerWeight.Value);
            }
        }
    }
    
    [HarmonyPrefix]
    [HarmonyPatch("SetSpraycanState")]
    private static bool SetSpraycanState(Player __instance, Player.SpraycanState state) {
        var associatedPlayer = Plugin.PlayerManager.GetAssociatedPlayer(__instance);
        if (associatedPlayer is not null) {
            return Plugin.PlayerManager.IsSettingVisual;
        }
        
        if (__instance == WorldHandler.instance?.GetCurrentPlayer()) {
            Plugin.PlayerManager.IsVisualRefreshQueued = true;
        }

        return true;
    }
}
