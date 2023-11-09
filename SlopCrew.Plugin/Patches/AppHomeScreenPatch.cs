using System;
using System.Reflection;
using HarmonyLib;
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
        var traverse = Traverse.Create(__instance);
        var apps = traverse.Field<HomeScreenApp[]>("m_Apps");

        var arr = apps.Value;
        var newArr = new HomeScreenApp[arr.Length + 1];
        for (var i = 0; i < arr.Length; i++) newArr[i] = arr[i];

        var sprite = TextureLoader.LoadResourceAsSprite("SlopCrew.Plugin.res.phone_icon.png", 128, 128);

        var app = ScriptableObject.CreateInstance<HomeScreenApp>();
        var appTraverse = Traverse.Create(app);
        appTraverse.Field<string>("m_AppName").Value = "AppSlopCrew";
        appTraverse.Field<string>("m_DisplayName").Value = "slop crew";
        appTraverse.Field<Sprite>("m_AppIcon").Value = sprite;
        appTraverse.Field<HomeScreenApp.HomeScreenAppType>("appType").Value = (HomeScreenApp.HomeScreenAppType) 1337;

        newArr[newArr.Length - 1] = app;
        apps.Value = newArr;
    }

    [HarmonyPostfix]
    [HarmonyPatch("OnAppInit")]
    protected static void OnAppInit(AppHomeScreen __instance) {
        if (Plugin.SlopConfig.ShowConnectionInfo.Value) {
            __instance.gameObject.AddComponent<UIConnectionStatus>();
        }
    }
}
