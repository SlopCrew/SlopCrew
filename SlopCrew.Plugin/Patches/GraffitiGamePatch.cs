using System.Collections.Generic;
using HarmonyLib;
using Reptile;
using SlopCrew.Common.Network.Serverbound;

namespace SlopCrew.Plugin.Patches; 

[HarmonyPatch(typeof(GraffitiGame))]
public class GraffitiGamePatch {
    [HarmonyPostfix]
    [HarmonyPatch("SetState")]
    public static void SetState(GraffitiGame __instance, GraffitiGame.GraffitiGameState setState) {
        if (setState is GraffitiGame.GraffitiGameState.FINISHED) {
            var targetsHitSequence = Traverse.Create(__instance).Field<List<int>>("targetsHitSequence").Value;
            
            Plugin.NetworkConnection.SendMessage(new ServerboundGraffitiPaint() {
                GraffitiSpot = Plugin.TargetedGraffitiSpot.gameObject.name,
                targetsHitSequence = targetsHitSequence
            });
        }
    }
}
