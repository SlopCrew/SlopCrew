using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace SlopCrew.Plugin;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInProcess("Bomb Rush Cyberfunk.exe")]
public class Plugin : BaseUnityPlugin {
    public static ManualLogSource Log = null!;
    public static Harmony Harmony = null!;

    public static NetworkConnection NetworkConnection = null!;
    public static PlayerManager PlayerManager = null!;

    public static ConfigEntry<string> ConfigAddress = null!;
    public static ConfigEntry<string> ConfigUsername = null!;
    public static ConfigEntry<bool> ConfigShowConnectionInfo = null!;
    public static ConfigEntry<bool> ConfigShowPlayerNameplates = null!;
    public static ConfigEntry<bool> ConfigBillboardNameplates = null!;

    public static bool IsConnected = false;
    public static int PlayerCount = 0;

    private void Awake() {
        Log = this.Logger;
        Application.runInBackground = true;

        this.SetupHarmony();
        this.SetupConfig();

        NetworkConnection = new();
        PlayerManager = new();

        //NetworkExtensions.Log = (msg) => { Log.LogInfo("NetworkExtensions Log " + msg); };
    }

    private void OnDestroy() {
        PlayerManager.Dispose();
    }

    private void SetupHarmony() {
        Harmony = new Harmony("SlopCrew.Plugin.Harmony");

        var patches = typeof(Plugin).Assembly.GetTypes()
                                    .Where(m => m.GetCustomAttributes(typeof(HarmonyPatch), false).Length > 0)
                                    .ToArray();

        foreach (var patch in patches) {
            Harmony.PatchAll(patch);
        }
    }

    private void SetupConfig() {
        ConfigAddress = this.Config.Bind(
            "Server",
            "Address",
            "ws://lmaobox.n2.pm:1337/",
            "Address of the server to connect to, in WebSocket format."
        );

        ConfigUsername = this.Config.Bind(
            "General",
            "Username",
            "Big Slopper",
            "Username to show to other players."
        );

        ConfigShowConnectionInfo = this.Config.Bind(
            "General",
            "ShowConnectionInfo",
            true,
            "Show current connection status and player count."
        );

        ConfigShowPlayerNameplates = this.Config.Bind(
            "General",
            "ShowPlayerNameplates",
            true,
            "Show players' names above their heads."
        );
        
        ConfigBillboardNameplates = this.Config.Bind(
            "General",
            "BillboardNameplates",
            true,
            "Billboard nameplates (always face the camera)."
        );
    }
}
