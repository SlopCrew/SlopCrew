using System.Collections.Generic;
using BepInEx.Configuration;

namespace SlopCrew.Plugin;

public class SlopConfigFile {
    private readonly ConfigFile config;

    // Server
    public ConfigEntry<string> Address;
    public ConfigEntry<string> SecretCode;

    // General
    public ConfigEntry<string> Username;
    public ConfigEntry<bool> ShowConnectionInfo;
    public ConfigEntry<bool> ShowPlayerNameplates;
    public ConfigEntry<bool> BillboardNameplates;
    public ConfigEntry<bool> OutlineNameplates;
    public ConfigEntry<bool> ShowPlayerMapPins;

    // Phone
    public ConfigEntry<bool> ReceiveNotifications;
    public ConfigEntry<bool> StartEncountersOnRequest;

    // Fixes
    public ConfigEntry<bool> FixBikeGate;
    public ConfigEntry<bool> FixAmbientColors;

    public SlopConfigFile(ConfigFile config) {
        this.config = config;

        // Server
        this.Address = this.config.Bind(
            "Server",
            "Address",
            "wss://sloppers.club/",
            "Address of the server to connect to, in WebSocket format."
        );

        // Migrate old servers to the new server
        var oldAddresses = new List<string> {
            "ws://lmaobox.n2.pm:1337/",
            "wss://slop.n2.pm/"
        };
        if (oldAddresses.Contains(this.Address.Value)) {
            Plugin.Log.LogInfo("Migrating address to new server");
            this.Address.Value = "wss://sloppers.club/";
            this.config.Save();
        }

        this.SecretCode = this.config.Bind(
            "Server",
            "SecretCode",
            "",
            "Don't worry about it."
        );

        // General
        this.Username = this.config.Bind(
            "General",
            "Username",
            "Big Slopper",
            "Username to show to other players."
        );

        this.ShowConnectionInfo = this.config.Bind(
            "General",
            "ShowConnectionInfo",
            true,
            "Show current connection status and player count."
        );

        this.ShowPlayerNameplates = this.config.Bind(
            "General",
            "ShowPlayerNameplates",
            true,
            "Show players' names above their heads."
        );

        this.BillboardNameplates = this.config.Bind(
            "General",
            "BillboardNameplates",
            true,
            "Billboard nameplates (always face the camera)."
        );

        this.OutlineNameplates = this.config.Bind(
            "General",
            "OutlineNameplates",
            true,
            "Add a dark outline to nameplates for contrast."
        );

        this.ShowPlayerMapPins = this.config.Bind(
            "General",
            "ShowPlayerMapPins",
            true,
            "Show players on the phone map."
        );

        // Phone
        this.ReceiveNotifications = this.config.Bind(
            "Phone",
            "ReceiveNotifications",
            true,
            "Receive notifications to start encounters from other players."
        );
        this.StartEncountersOnRequest = this.config.Bind(
            "Phone",
            "StartEncountersOnRequest",
            true,
            "Start encounters when opening a notification."
        );

        // Fixes
        this.FixBikeGate = this.config.Bind(
            "Fixes",
            "FixBikeGate",
            true,
            "Fix other players being able to start bike gate cutscenes."
        );

        this.FixAmbientColors = this.config.Bind(
            "Fixes",
            "FixAmbientColors",
            true,
            "Fix other players being able to change color grading."
        );
    }
}
