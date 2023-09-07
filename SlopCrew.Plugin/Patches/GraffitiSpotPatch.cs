using System.Collections.Generic;
using HarmonyLib;
using Reptile;
using SlopCrew.Common;
using SlopCrew.Common.Network.Serverbound;
using SlopCrew.Plugin.Encounters;
using UnityEngine;
using Player = Reptile.Player;

namespace SlopCrew.Plugin.Patches;

[HarmonyPatch(typeof(GraffitiSpot))]
public class GraffitiSpotPatch {
    [HarmonyPrefix]
    [HarmonyPatch("Paint")]
    public static bool Paint(GraffitiSpot __instance, Crew newCrew, GraffitiArt graffitiArt, Player byPlayer) {
        if (Plugin.CurrentEncounter is SlopGraffitiEncounter) {
            
            Plugin.Log.LogInfo("CALLING PAINT IN THE SLOP GRAFFITI SPOT");
            Plugin.TargetedGraffitiSpot.Paint(graffitiArt, byPlayer);
            return false;
        }

        return true;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch("GiveRep")]
    public static bool GiveRep(GraffitiSpot __instance, bool noRepPickup, bool oldBottomWasClaimedByPlayableCrew) {
        if (Plugin.CurrentEncounter is SlopGraffitiEncounter) {
            
            Plugin.Log.LogInfo("SKIPPING GIVEREP BECAUSE WE ARE IN SLOP GRAFFITI ENCOUNTER");
            return false;
        }

        return true;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch("SpawnRep")]
    public static bool SpawnRep(GraffitiSpot __instance) {
        if (Plugin.CurrentEncounter is SlopGraffitiEncounter) {
            
            Plugin.Log.LogInfo("SKIPPING SPAWNREP BECAUSE WE ARE IN SLOP GRAFFITI ENCOUNTER");
            return false;
        }

        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch("SetState")]
    public static bool SetState(GraffitiSpot __instance, GraffitiState setState) {
        if (Plugin.CurrentEncounter is SlopGraffitiEncounter) {

            if (setState == GraffitiState.FINISHED)
                Traverse.Create(__instance).Field<bool>("inGraffitiMode").Value = false;
            return false;
        }

        return true;
    }
}
