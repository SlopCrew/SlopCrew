using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SlopCrew.Common.Network;

namespace SlopCrew.Plugin;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInProcess("Bomb Rush Cyberfunk.exe")]
public class Plugin : BaseUnityPlugin {
    public static ManualLogSource Log;
    public static Harmony Harmony;

    public static NetworkConnection NetworkConnection;
    public static PlayerManager PlayerManager;

    private void Awake() {
        Log = this.Logger;
        this.SetupHarmony();

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

    private void Update() {
        PlayerManager.Update();
    }
}
