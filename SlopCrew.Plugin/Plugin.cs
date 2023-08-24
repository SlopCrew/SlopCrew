using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

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

    private void Awake() {
        Log = this.Logger;

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
    }
}
