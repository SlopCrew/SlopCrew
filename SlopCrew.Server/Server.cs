using Serilog;
using Serilog.Core;
using SlopCrew.Common.Network.Clientbound;
using WebSocketSharp.Server;

namespace SlopCrew.Server;

public class Server {
    public static Server Instance = new();
    public static Logger Logger = null!;

    private string interfaceStr;
    private WebSocketServer wsServer;

    public Server() {
        this.interfaceStr = Environment.GetEnvironmentVariable("SLOP_INTERFACE") ?? "ws://0.0.0.0:42069";
        var debug = Environment.GetEnvironmentVariable("SLOP_DEBUG") == "true";

        this.wsServer = new WebSocketServer(this.interfaceStr);
        this.wsServer.AddWebSocketService<ServerConnection>("/");

        var logger = new LoggerConfiguration().WriteTo.Console();
        if (debug) logger = logger.MinimumLevel.Verbose();
        Logger = logger.CreateLogger();
        Log.Logger = Logger;
    }

    public void Start() {
        this.wsServer.Start();
        Log.Information("Listening on {Interface} - press any key to close", this.interfaceStr);
    
        if (Console.IsInputRedirected) {
            // If running in a non-interactive environment (like Docker), just wait indefinitely
            while (true) {
                System.Threading.Thread.Sleep(10000);
            }
        } else {
            Console.ReadKey();
            this.wsServer.Stop();
        }
    }


    public void TrackConnection(ServerConnection conn) {
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

    public void UntrackConnection(ServerConnection conn) {
        var player = conn.Player;

        // Don't bother untracking someone we never tracked in the first place
        if (player is null) return;

        // Contains checks just in case we get into this state somehow
        this.BroadcastNewPlayers(player.Stage, conn);
    }

    private void BroadcastNewPlayers(int stage, ServerConnection? exclude = null) {
        var connections = this.GetConnections()
                              .Where(x => x.Player?.Stage == stage)
                              .ToList();

        foreach (var connection in connections) {
            var players = connections
                          .Where(x =>
                                     x.Player?.ID != connection.Player?.ID
                                     && x.ID != exclude?.ID)
                          .Select(c => c.Player)
                          .ToList();

            connection.Send(new ClientboundPlayersUpdate {
                Players = players!
            });
        }
    }

    public uint GetNextID() {
        var ids = this.GetConnections().Select(x => x.Player?.ID).ToList();
        var id = 0u;
        while (ids.Contains(id)) id++;
        return id;
    }

    public List<ServerConnection> GetConnections() {
        var service = this.wsServer.WebSocketServices["/"];
        return service.Sessions.Sessions
                      .Cast<ServerConnection>()
                      .ToList();
    }
}
