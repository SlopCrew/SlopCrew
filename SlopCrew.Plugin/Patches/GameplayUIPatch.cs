using HarmonyLib;
using Reptile;
using SlopCrew.Plugin.UI;

namespace SlopCrew.Plugin.Patches; 

[HarmonyPatch(typeof(GameplayUI))]
public class GameplayUIPatch {
    [HarmonyPostfix]
    [HarmonyPatch("Init")]
    public static void Init(GameplayUI __instance) {
        if (Plugin.ConfigUIShowConnectionInfo.Value) {
            __instance.gameplayScreen.gameObject.AddComponent<UIConnectionStatus>();
        }
    }
}
