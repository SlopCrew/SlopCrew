using Graphite;
using Serilog;

namespace SlopCrew.Server;

public class Metrics {
    public int Connections = 0;
    public Dictionary<int, int> Population = new();

    private GraphiteTcpClient? graphite;

    public Metrics(Config config) {
        if (config.Graphite.Host != null) {
            Log.Information("Connecting to Graphite ({Host}:{Port})...", config.Graphite.Host, config.Graphite.Port);
            try {
                this.graphite = new GraphiteTcpClient(
                    config.Graphite.Host,
                    config.Graphite.Port,
                    "slop"
                );
            } catch (Exception e) {
                Log.Error(e, "Error connecting to Graphite");
                this.graphite = null;
            }
        }
    }

    public void UpdateConnections(int count) {
        Log.Information("Now at {ConnectionCount} connections", count);
        this.Connections = count;
        this.graphite?.Send("connections", count);
    }

    public void UpdatePopulation(int stage, int count) {
        Log.Information("Now at {PopulationCount} players in stage {Stage}", count, stage);
        this.Population[stage] = count;
        this.graphite?.Send($"population.{stage}", count);
    }
}
