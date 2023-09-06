using Serilog;
using Tomlyn;

namespace SlopCrew.Server;

public class Config {
    public string Interface { get; set; } = "http://+:42069";
    public bool Debug { get; set; } = false;
    public string? AdminPassword { get; set; } = null;

    public ConfigCertificates Certificates { get; set; } = new();
    public ConfigGraphite Graphite { get; set; } = new();
    public ConfigEncounters Encounters { get; set; } = new();

    public class ConfigCertificates {
        public string? Path { get; set; } = null;
        public string? Password { get; set; } = null;
    }

    public class ConfigGraphite {
        public string? Host { get; set; } = null;
        public int Port { get; set; } = 2003;
    }

    public class ConfigEncounters {
        public float ScoreDuration { get; set;  } = 90f;
        public float ComboDuration { get; set; } = 300f;
    }

    public static Config ResolveConfig(string? filePath) {
        try {
            if (filePath is not null) {
                if (File.Exists(filePath)) return Toml.ToModel<Config>(File.ReadAllText(filePath));

                Log.Error("Config at {Path} (set from CLI argument) does not exist, falling back to default",
                          filePath);
                return new Config();
            }

            var env = Environment.GetEnvironmentVariable("SLOP_CONFIG")?.Trim().ToLower();
            if (env is not null) {
                if (File.Exists(env)) return Toml.ToModel<Config>(File.ReadAllText(env));

                Log.Error("Config at {Path} (set from environment) does not exist, falling back to default", env);
                return new Config();
            }

            var onFilesystem = Path.Combine(Environment.CurrentDirectory, "config.toml");
            if (File.Exists(onFilesystem)) return Toml.ToModel<Config>(File.ReadAllText(onFilesystem));

            Log.Warning("No config file supplied - falling back to default");
            return new Config();
        } catch (Exception e) {
            Log.Error(e, "Error while loading config, falling back to default");
            return new Config();
        }
    }
}
