using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SlopCrew.API;
using UnityEngine;

namespace SlopCrew.Plugin;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInProcess("Bomb Rush Cyberfunk.exe")]
public class Plugin : BaseUnityPlugin {
    public static ManualLogSource Log = null!;
    public static Harmony Harmony = null!;

    public static NetworkConnection NetworkConnection = null!;
    public static PlayerManager PlayerManager = null!;
    public static SlopCrewAPI API = null!;

    public static bool IsConnected = false;
    public static int PlayerCount = 0;
    
    // START ===== CONFIG VALUES ===== START \\
    
    // General
    public static ConfigEntry<string> ConfigAddress = null!;
    public static ConfigEntry<string> ConfigUsername = null!;
    public static ConfigEntry<string> ConfigSecretCode = null!;
    
    // UI
    public static ConfigEntry<bool> ConfigUIShowConnectionInfo = null!;
    public static ConfigEntry<bool> ConfigUIShowPlayerNameplates = null!;
    public static ConfigEntry<bool> ConfigUIBillboardNameplates = null!;
    public static ConfigEntry<bool> ConfigUIShowPlayerPins = null!;
    
    // Cutscenes
    public static ConfigEntry<bool> ConfigCutsceneDisablePolice = null!;
    public static ConfigEntry<bool> ConfigCutsceneDisableBikeGate = null!;
    
    // END ===== CONFIG VALUES ===== END \\
    
    private void Awake() {
        Log = this.Logger;
        Application.runInBackground = true;

        this.SetupHarmony();
        this.SetupConfig();
        
        API = new();
        APIManager.RegisterAPI(API);

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
            "wss://slop.n2.pm/",
            "Address of the server to connect to, in WebSocket format."
        );

        ConfigUsername = this.Config.Bind(
            "General",
            "Username",
            "Big Slopper",
            "Username to show to other players."
        );
        
        ConfigSecretCode = this.Config.Bind(
            "Server",
            "SecretCode",
            "",
            "Don't worry about it."
        );

        ConfigUIShowConnectionInfo = this.Config.Bind(
            "General",
            "ShowConnectionInfo",
            true,
            "Show current connection status and player count."
        );

        ConfigUIShowPlayerNameplates = this.Config.Bind(
            "General",
            "ShowPlayerNameplates",
            true,
            "Show players' names above their heads."
        );

        ConfigUIBillboardNameplates = this.Config.Bind(
            "General",
            "BillboardNameplates",
            true,
            "Billboard nameplates (always face the camera)."
        );

        ConfigUIShowPlayerPins = this.Config.Bind(
            "General",
            "ShowPlayerMapPins",
            true,
            "Show players on the phone map."
        );

        ConfigCutsceneDisablePolice = this.Config.Bind(
            "General",
            "DisablePoliceCutscene",
            true,
            "Disable cutscenes when the heat level changes."
        );

        ConfigCutsceneDisableBikeGate = this.Config.Bind(
            "General",
            "DisableBikeGateCutscene",
            true,
            "Disable the bike gate opening cutscene."
        );
    }
}
