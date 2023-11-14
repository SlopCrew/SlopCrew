using BepInEx.Configuration;

namespace SlopCrew.Plugin;

public class Config(ConfigFile config) {
    public ConfigGeneral General = new(config);
    public ConfigFixes Fixes = new(config);
    public ConfigPhone Phone = new(config);
    public ConfigServer Server = new(config);

    public class ConfigGeneral(ConfigFile config) {
        public ConfigEntry<string> Username = config.Bind(
            "General",
            "Username",
            "Big Slopper",
            "Username to show to other players."
        );

        public ConfigEntry<bool> ShowConnectionInfo = config.Bind(
            "General",
            "ShowConnectionInfo",
            true,
            "Show current connection status and player count."
        );

        public ConfigEntry<bool> ShowPlayerNameplates = config.Bind(
            "General",
            "ShowPlayerNameplates",
            true,
            "Show players' names above their heads."
        );

        public ConfigEntry<bool> BillboardNameplates = config.Bind(
            "General",
            "BillboardNameplates",
            true,
            "Billboard nameplates (always face the camera)."
        );

        public ConfigEntry<bool> OutlineNameplates = config.Bind(
            "General",
            "OutlineNameplates",
            true,
            "Add a dark outline to nameplates for contrast."
        );

        public ConfigEntry<bool> ShowPlayerMapPins = config.Bind(
            "General",
            "ShowPlayerMapPins",
            true,
            "Show players on the phone map."
        );
    }

    public class ConfigFixes(ConfigFile config) {
        public ConfigEntry<bool> FixBikeGate = config.Bind(
            "Fixes",
            "FixBikeGate",
            true,
            "Fix other players being able to start bike gate cutscenes."
        );

        public ConfigEntry<bool> FixAmbientColors = config.Bind(
            "Fixes",
            "FixAmbientColors",
            true,
            "Fix other players being able to change color grading."
        );
    }

    public class ConfigPhone(ConfigFile config) {
        public ConfigEntry<bool> ReceiveNotifications = config.Bind(
            "Phone",
            "ReceiveNotifications",
            true,
            "Receive notifications to start encounters from other players."
        );

        public ConfigEntry<bool> StartEncountersOnRequest = config.Bind(
            "Phone",
            "StartEncountersOnRequest",
            true,
            "Start encounters when opening a notification."
        );
    }

    public class ConfigServer(ConfigFile config) {
        public ConfigEntry<string> Host = config.Bind(
            "Server",
            "Host",
            "sloppers.club",
            "Host to connect to. This can be an IP address or domain name."
        );

        public ConfigEntry<ushort> Port = config.Bind(
            "Server",
            "Port",
            (ushort) 42069,
            "Port to connect to."
        );
    }
}
