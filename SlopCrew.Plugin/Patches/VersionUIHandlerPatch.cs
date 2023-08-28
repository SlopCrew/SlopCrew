using HarmonyLib;
using Reptile;
using TMPro;

namespace SlopCrew.Plugin.Patches;

[HarmonyPatch(typeof(VersionUIHandler))]
public class VersionUIHandlerPatch {
    [HarmonyPostfix]
    [HarmonyPatch("SetVersionText")]
    private static void SetVersionText(VersionUIHandler __instance) {
        var obj = __instance.versionText;
        var verText = obj.text;
        
        obj.alignment = TextAlignmentOptions.BottomLeft;
        obj.text = $"<color=\"purple\">SlopCrew v{PluginInfo.PLUGIN_VERSION} - {Plugin.ConfigUsername.Value}\n" + verText;
    }
}
