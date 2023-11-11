using BepInEx.Configuration;

namespace SlopCrew.Plugin;

public class Config {
    public ConfigGeneral General;
    public ConfigFixes Fixes;
    public ConfigPhone Phone;
    public ConfigServer Server;

    public Config(ConfigFile config) {
        this.General = new ConfigGeneral(config);
        this.Fixes = new ConfigFixes(config);
        this.Phone = new ConfigPhone(config);
        this.Server = new ConfigServer(config);
    }

    public class ConfigGeneral {
        public ConfigEntry<string> Username;
        public ConfigEntry<bool> ShowConnectionInfo;
        public ConfigEntry<bool> ShowPlayerNameplates;
        public ConfigEntry<bool> BillboardNameplates;
        public ConfigEntry<bool> OutlineNameplates;
        public ConfigEntry<bool> ShowPlayerMapPins;

        public ConfigGeneral(ConfigFile config) {
            this.Username = config.Bind(
                "General",
                "Username",
                "Big Slopper",
                "Username to show to other players."
            );

            this.ShowConnectionInfo = config.Bind(
                "General",
                "ShowConnectionInfo",
                true,
                "Show current connection status and player count."
            );

            this.ShowPlayerNameplates = config.Bind(
                "General",
                "ShowPlayerNameplates",
                true,
                "Show players' names above their heads."
            );

            this.BillboardNameplates = config.Bind(
                "General",
                "BillboardNameplates",
                true,
                "Billboard nameplates (always face the camera)."
            );

            this.OutlineNameplates = config.Bind(
                "General",
                "OutlineNameplates",
                true,
                "Add a dark outline to nameplates for contrast."
            );

            this.ShowPlayerMapPins = config.Bind(
                "General",
                "ShowPlayerMapPins",
                true,
                "Show players on the phone map."
            );
        }
    }

    public class ConfigFixes {
        public ConfigEntry<bool> FixBikeGate;
        public ConfigEntry<bool> FixAmbientColors;

        public ConfigFixes(ConfigFile config) {
            this.FixBikeGate = config.Bind(
                "Fixes",
                "FixBikeGate",
                true,
                "Fix other players being able to start bike gate cutscenes."
            );

            this.FixAmbientColors = config.Bind(
                "Fixes",
                "FixAmbientColors",
                true,
                "Fix other players being able to change color grading."
            );
        }
    }

    public class ConfigPhone {
        public ConfigEntry<bool> ReceiveNotifications;
        public ConfigEntry<bool> StartEncountersOnRequest;

        public ConfigPhone(ConfigFile config) {
            this.ReceiveNotifications = config.Bind(
                "Phone",
                "ReceiveNotifications",
                true,
                "Receive notifications to start encounters from other players."
            );

            this.StartEncountersOnRequest = config.Bind(
                "Phone",
                "StartEncountersOnRequest",
                true,
                "Start encounters when opening a notification."
            );
        }
    }

    public class ConfigServer {
        public ConfigEntry<string> Host;
        public ConfigEntry<ushort> Port;

        public ConfigServer(ConfigFile config) {
            this.Host = config.Bind(
                "Server",
                "Host",
                "sloppers.club",
                "Host to connect to. This can be an IP address or domain name."
            );

            this.Port = config.Bind(
                "Server",
                "Port",
                (ushort) 42069,
                "Port to connect to."
            );
        }
    }
}
