using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SlopCrew.API;
using SlopCrew.Plugin.Encounters;
using SlopCrew.Plugin.Scripts.Race;
using SlopCrew.Plugin.UI.Phone;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace SlopCrew.Plugin;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInProcess("Bomb Rush Cyberfunk.exe")]
public class Plugin : BaseUnityPlugin {
    public static ManualLogSource Log = null!;
    public static DebugLog DebugLog = null!;
    public static Harmony Harmony = null!;
    public static SlopConfigFile SlopConfig = null!;

    public static CharacterInfoManager CharacterInfoManager = null!;
    public static NetworkConnection NetworkConnection = null!;
    public static PlayerManager PlayerManager = null!;
    public static RaceManager RaceManager = null!;
    public static SlopCrewAPI API = null!;
    public static SlopEncounter? CurrentEncounter;
    public static PhoneInitializer PhoneInitializer = null!;

    private static int shouldIgnoreInput = 0;

    public static bool ShouldIgnoreInput {
        get => Interlocked.CompareExchange(ref shouldIgnoreInput, 0, 0) == 1;
        set => Interlocked.Exchange(ref shouldIgnoreInput, value ? 1 : 0);
    }


    private void Awake() {
        Log = this.Logger;
        DebugLog = new();
        SlopConfig = new(this.Config);
        Application.runInBackground = true;

        this.SetupHarmony();

        API = new();
        APIManager.RegisterAPI(API);

        CharacterInfoManager = new();
        NetworkConnection = new();
        PlayerManager = new();
        RaceManager = new();
        PhoneInitializer = new();

        //NetworkExtensions.Log = (msg) => { Log.LogInfo("NetworkExtensions Log " + msg); };
    }

    private void OnDestroy() {
        PlayerManager.Dispose();
        DebugLog.Dispose();
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
