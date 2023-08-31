using Serilog;
using Serilog.Core;
using SlopCrew.Common;
using SlopCrew.Common.Network.Clientbound;
using EmbedIO;
using EmbedIO.WebApi;
using Graphite;
using Constants = SlopCrew.Common.Constants;

namespace SlopCrew.Server;

public class Server {
    public static Server Instance = null!;
    public static Logger Logger = null!;
    public static uint CurrentTick;

    private Config config;

    public WebServer WebServer;
    public SlopWebSocketModule Module;
    public Metrics Metrics;

    public Server(string[] args) {
        var logger = new LoggerConfiguration().WriteTo.Console();
        Logger = logger.CreateLogger();
        Log.Logger = Logger;

        this.config = Config.ResolveConfig(args.Length > 0 ? args[0] : null);
        if (this.config.Debug) {
            var newLogger = new LoggerConfiguration().WriteTo.Console()
                                                     .MinimumLevel.Verbose()
                                                     .CreateLogger();
            Logger = newLogger;
            Log.Logger = Logger;
        }
        
        this.Metrics = new Metrics(this.config);
        this.Module = new SlopWebSocketModule();

        this.WebServer = new WebServer(o => {
            var certPath = this.config.Certificates.Path;
            var certPass = this.config.Certificates.Password;

            if (this.config.Interface.StartsWith("https:")) {
                if (File.Exists(certPath)) {
                    o.WithCertificate(
                        new System.Security.Cryptography.X509Certificates.X509Certificate2(
                            certPath, certPass));
                } else {
                    Log.Error("Certificate {Path} does not exist, falling back to HTTP", certPath);
                    this.config.Interface = this.config.Interface.Replace("https:", "http:");
                }
            }

            o.WithUrlPrefix(this.config.Interface);
            o.WithMode(HttpListenerMode.EmbedIO);
        });

        // my editorconfig sucks and indents it a lot so let's do this on a separate statement
        this.WebServer = this.WebServer
                             // API goes before websocket or it gets eaten
                             .WithWebApi("/api", m => m.WithController<SlopAPIController>())
                             .WithModule(this.Module);
    }

    public void Start() {
        Log.Information("Listening on {Interface} - press any key to close", this.config.Interface);

        // ReSharper disable once FunctionNeverReturns
        new Thread(() => {
            const int tickRate = (int) (Constants.TickRate * 1000);
            while (true) {
                Thread.Sleep(tickRate);

                try {
                    lock (this.Module.Connections) {
                        this.RunTick();
                    }
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

        this.Metrics.UpdatePopulation(stage, allPlayersInStage.Count);

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
        lock (this.Module.Connections) {
            var ids = new HashSet<uint>(this.GetConnections().Select(x => x.Player?.ID).Where(id => id.HasValue)
                                            .Cast<uint>());
            uint id = 0;
            while (ids.Contains(id)) {
                id++;
            }
            return id;
        }
    }

    public IEnumerable<ConnectionState> GetConnections() {
        return this.Module.Connections.Values;
    }
}
