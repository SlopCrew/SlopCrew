using HarmonyLib;
using Reptile.Phone;
using SlopCrew.Plugin.UI;

namespace SlopCrew.Plugin.Patches; 

[HarmonyPatch(typeof(AppHomeScreen))]
public class AppHomeScreenPatch {
    [HarmonyPostfix]
    [HarmonyPatch("OnAppInit")]
    protected static void OnAppInit(AppHomeScreen __instance) {
        if (Plugin.SlopConfig.ShowConnectionInfo.Value) {
            __instance.gameObject.AddComponent<UIConnectionStatus>();
        }
    }
}
