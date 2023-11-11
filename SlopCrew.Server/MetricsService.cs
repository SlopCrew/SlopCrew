using Graphite;
using Microsoft.Extensions.Options;
using SlopCrew.Server.Options;

namespace SlopCrew.Server;

public class MetricsService {
    private GraphiteTcpClient? graphite;
    private ILogger<MetricsService> logger;

    public int Connections { get; private set; } = 0;
    public Dictionary<int, int> Population { get; private set; } = new();

    public MetricsService(
        ILogger<MetricsService> logger,
        IOptions<GraphiteOptions> options
    ) {
        this.logger = logger;

        if (options.Value.Host != null) {
            this.logger.LogInformation(
                "Connecting to Graphite ({Host}:{Port})",
                options.Value.Host,
                options.Value.Port
            );

            try {
                this.graphite = new GraphiteTcpClient(
                    options.Value.Host,
                    options.Value.Port,
                    "slop"
                );
            } catch (Exception e) {
                this.logger.LogError(e, "Error connecting to Graphite");
                this.graphite = null;
            }
        }
    }

    public void UpdateConnections(int count) {
        this.logger.LogInformation("Now at {Count} connections", count);
        this.Connections = count;
        this.graphite?.Send("connections", count);
    }

    public void UpdatePopulation(int stage, int count) {
        this.logger.LogInformation("Now at {Count} players in stage {Stage}", count, stage);
        this.Population[stage] = count;
        this.graphite?.Send($"population.{stage}", count);
    }

    public void SubmitPluginVersionMetrics(List<string> pluginVersions) {
        var versionCounts = pluginVersions.GroupBy(x => x).ToDictionary(x => x.Key, x => x.Count());
        foreach (var (version, count) in versionCounts) {
            var sanitizedVersion = version.Replace('.', '_');
            this.graphite?.Send($"plugin_version.{sanitizedVersion}", count);
        }
    }
}
