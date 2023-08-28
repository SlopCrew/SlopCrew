using Serilog;
using Serilog.Core;
using SlopCrew.Common;
using SlopCrew.Common.Network.Clientbound;
using EmbedIO;
using EmbedIO.WebSockets;
using Graphite;
using Constants = SlopCrew.Common.Constants;

namespace SlopCrew.Server;

public class Server {
    public static Server Instance = new();
    public static Logger Logger = null!;
    public static uint CurrentTick;

    private string interfaceStr;
    private bool debug;

    public WebServer WebServer;
    public SlopWebSocketModule Module;
    public GraphiteTcpClient? Graphite;

    public Server() {
        this.interfaceStr = Environment.GetEnvironmentVariable("SLOP_INTERFACE") ?? "http://+:42069";
        var certificatePath = Environment.GetEnvironmentVariable("SLOP_CERTIFICATE_PATH") ?? "./cert/cert.pfx";
        var certificatePass = Environment.GetEnvironmentVariable("SLOP_CERTIFICATE_PASS") ?? null;

        var debugStr = Environment.GetEnvironmentVariable("SLOP_DEBUG")?.Trim().ToLower();
        this.debug = int.TryParse(debugStr, out var debugInt) ? debugInt != 0 : debugStr == "true";

        var graphiteStr = Environment.GetEnvironmentVariable("SLOP_GRAPHITE")?.Trim().ToLower();
        if (graphiteStr != null) {
            var graphitePortStr = Environment.GetEnvironmentVariable("SLOP_GRAPHITE_PORT")?.Trim().ToLower();
            var graphitePort = int.TryParse(graphitePortStr, out var graphitePortInt) ? graphitePortInt : 2003;
            this.Graphite = new GraphiteTcpClient(graphiteStr, graphitePort, "slop");
        }

        var logger = new LoggerConfiguration().WriteTo.Console();
        if (this.debug) logger = logger.MinimumLevel.Verbose();
        Logger = logger.CreateLogger();
        Log.Logger = Logger;

        this.Module = new SlopWebSocketModule();
        this.WebServer = new WebServer(o => {
            if (interfaceStr.StartsWith("https:")) {
                if (File.Exists(certificatePath)) {
                    o.WithCertificate(
                        new System.Security.Cryptography.X509Certificates.X509Certificate2(
                            certificatePath, certificatePass));
                } else {
                    Log.Error("Certificate {Path} does not exist, falling back to HTTP", certificatePath);
                    interfaceStr = interfaceStr.Replace("https:", "http:");
                }
            }

            o.WithUrlPrefix(this.interfaceStr);
            o.WithMode(HttpListenerMode.EmbedIO);
        }).WithModule(this.Module);
    }

    public void Start() {
        Log.Information("Listening on {Interface} - press any key to close", this.interfaceStr);

        // ReSharper disable once FunctionNeverReturns
        new Thread(() => {
            const int tickRate = (int) (Constants.TickRate * 1000);
            while (true) {
                Thread.Sleep(tickRate);

                try {
                    this.RunTick();
                } catch (Exception e) {
                    Log.Error(e, "Error while running tick");
                }
            }
        }).Start();

        this.WebServer.Start();
    }

    private void RunTick() {
        // Go through each connection and run their respective ticks.
        foreach (var connection in this.GetConnections()) {
            connection.RunTick();
        }

        // Increment the global tick counter.
        CurrentTick++;

        // Send the sync message every 50 ticks.
        if (CurrentTick % 50 == 0) {
            SendSyncToAllConnections(CurrentTick);
        }

        // Broadcast batched position updates - this is Jank:tm:
        var updates = new Dictionary<uint, Transform>();
        foreach (var connection in this.GetConnections()) {
            if (connection.QueuedPositionUpdate is not null) {
                updates.Add(connection.Player!.ID, connection.QueuedPositionUpdate);
                connection.QueuedPositionUpdate = null;
            }
        }

        if (updates.Count > 0) {
            var serialized = new ClientboundPlayerPositionUpdate {
                Positions = updates
            }.Serialize();

            foreach (var connection in this.GetConnections()) {
                this.Module.SendToContext(connection.Context, serialized);
            }
        }
    }

    private void SendSyncToAllConnections(uint tick) {
        var syncMessage = new ClientboundSync {
            ServerTickActual = tick
        };
        var serialized = syncMessage.Serialize();

        foreach (var connection in this.GetConnections()) {
            this.Module.SendToContext(connection.Context, serialized);
        }
    }

    public void TrackConnection(ConnectionState conn) {
        var player = conn.Player;
        if (player is null) {
            Log.Warning("TrackConnection but player is null? {Connection}", conn.DebugName());
            return;
        }

        // Remove from the old stage if crossing into a new one
        if (conn.LastStage != null && conn.LastStage != player.Stage) {
            this.BroadcastNewPlayers(conn.LastStage.Value);
        }

        // ...and broadcast into the new one
        this.BroadcastNewPlayers(player.Stage);
    }

    public void UntrackConnection(ConnectionState conn) {
        var player = conn.Player;

        // Don't bother untracking someone we never tracked in the first place
        if (player is null) return;

        // Contains checks just in case we get into this state somehow
        this.BroadcastNewPlayers(player.Stage, conn);
    }

    private void BroadcastNewPlayers(int stage, ConnectionState? exclude = null) {
        var connections = this.GetConnections()
                              .Where(x => x.Player?.Stage == stage)
                              .ToList();

        // Precalculate this outside the loop and filter out null players
        var allPlayersInStage = connections.Select(c => c.Player)
                                           .Where(p => p != null)
                                           .Cast<Player>()
                                           .ToList();

        this.Graphite?.Send($"population.{stage}", allPlayersInStage.Count);

        foreach (var connection in connections) {
            if (connection != exclude) {
                // This will be a list of all players except for the current one
                var playersToSend = allPlayersInStage
                                    .Where(p => p.ID != connection.Player?.ID)
                                    .ToList();

                this.Module.SendToContext(connection.Context, new ClientboundPlayersUpdate {
                    Players = playersToSend
                });
            }
        }
    }

    public uint GetNextID() {
        var ids = this.GetConnections().Select(x => x.Player?.ID).ToList();
        var id = 0u;
        while (ids.Contains(id)) id++;
        return id;
    }

    public List<ConnectionState> GetConnections() {
        var connections = this.Module.Connections;
        return connections.Values.ToList();
    }
}
