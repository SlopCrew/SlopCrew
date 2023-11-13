using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using Reptile;
using Reptile.Phone;
using SlopCrew.Plugin.UI;
using UnityEngine;

namespace SlopCrew.Plugin.Patches;

[HarmonyPatch(typeof(AppHomeScreen))]
public class AppHomeScreenPatch {
    [HarmonyPrefix]
    [HarmonyPatch("Awake")]
    public static void Awake(AppHomeScreen __instance) {
        var arr = __instance.m_Apps;
        var newArr = new HomeScreenApp[arr.Length + 1];
        for (var i = 0; i < arr.Length; i++) newArr[i] = arr[i];

        var sprite = TextureLoader.LoadResourceAsSprite("SlopCrew.Plugin.res.phone_icon.png", 128, 128);

        var app = ScriptableObject.CreateInstance<HomeScreenApp>();
        app.m_AppName = "AppSlopCrew";
        app.m_DisplayName = "slop crew";
        app.m_AppIcon = sprite;
        app.appType = (HomeScreenApp.HomeScreenAppType) 1337;

        newArr[newArr.Length - 1] = app;
        __instance.m_Apps = newArr;
    }

    [HarmonyPostfix]
    [HarmonyPatch("OnAppInit")]
    protected static void OnAppInit(AppHomeScreen __instance) {
        var config = Plugin.Host.Services.GetRequiredService<Config>();
        if (config.General.ShowConnectionInfo.Value) {
            __instance.gameObject.AddComponent<UIConnectionStatus>();
        }
    }
}
