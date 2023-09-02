using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SlopCrew.API;
using SlopCrew.Plugin.UI.Phone;
using UnityEngine;

namespace SlopCrew.Plugin;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInProcess("Bomb Rush Cyberfunk.exe")]
public class Plugin : BaseUnityPlugin {
    public static ManualLogSource Log = null!;
    public static Harmony Harmony = null!;
    public static SlopConfigFile SlopConfig = null!;

    public static NetworkConnection NetworkConnection = null!;
    public static PlayerManager PlayerManager = null!;
    public static SlopCrewAPI API = null!;
    public static SlopScoreEncounter SlopScoreEncounter = null!;
    public static SlopComboEncounter SlopComboEncounter = null!;
    public static PhoneInitializer PhoneInitializer = null!;

    private void Awake() {
        Log = this.Logger;
        SlopConfig = new(this.Config);
        Application.runInBackground = true;

        this.SetupHarmony();

        API = new();
        APIManager.RegisterAPI(API);

        NetworkConnection = new();
        PlayerManager = new();
        SlopScoreEncounter = new();
        SlopComboEncounter = new();
        PhoneInitializer = new();

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
}
