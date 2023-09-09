using HarmonyLib;
using Reptile;
using SlopCrew.Common;
using TMPro;
using UnityEngine;

namespace SlopCrew.Plugin.Patches;

[HarmonyPatch(typeof(VersionUIHandler))]
public class VersionUIHandlerPatch {
    [HarmonyPostfix]
    [HarmonyPatch("SetVersionText")]
    private static void SetVersionText(VersionUIHandler __instance) {
        var obj = __instance.versionText;
        var verText = obj.text;

        var origColor = obj.color;
        var hex = ColorUtility.ToHtmlStringRGB(origColor);

        obj.alignment = TextAlignmentOptions.BottomLeft;

        var username = PlayerNameFilter.DoFilter(Plugin.SlopConfig.Username.Value);
        obj.text = $"<color=\"purple\">SlopCrew v{PluginInfo.PLUGIN_VERSION} - <color=\"white\">{username}\n"
                   + $"<color=#{hex}>" + verText;
    }
}
