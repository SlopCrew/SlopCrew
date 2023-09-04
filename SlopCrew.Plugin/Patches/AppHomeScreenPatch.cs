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

        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SlopCrew.Plugin.res.phone_icon.png");
        if (stream is null) throw new Exception("Could not load phone icon");

        var bytes = new byte[stream.Length];
        var read = 0;
        while (read < bytes.Length) {
            read += stream.Read(bytes, read, bytes.Length - read);
        }

        var icon = new Texture2D(128, 128);
        icon.LoadImage(bytes);
        icon.Apply();
        var rect = new Rect(0, 0, icon.width, icon.height);
        var sprite = Sprite.Create(icon, rect, new Vector2(0.5f, 0.5f), 100);

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
