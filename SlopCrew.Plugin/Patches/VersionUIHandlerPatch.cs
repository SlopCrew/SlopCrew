using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
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

        var config = Plugin.Host.Services.GetRequiredService<Config>();
        var username = PlayerNameFilter.DoFilter(config.General.Username.Value);
        obj.text = $"<color=\"purple\">SlopCrew v{PluginInfo.PLUGIN_VERSION} - <color=\"white\">{username}\n"
                   + $"<color=#{hex}>" + verText;
    }
}
