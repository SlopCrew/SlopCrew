using Serilog;
using Serilog.Core;
using SlopCrew.Common.Network.Clientbound;
using WebSocketSharp.Server;

namespace SlopCrew.Server;

public class Server {
    public static Server Instance = new();
    public static Logger Logger = null!;

    public Dictionary<int, List<ServerConnection>> Players = new();
    public List<ServerConnection> Connections => this.Players.SelectMany(x => x.Value).ToList();

    private int port;
    private WebSocketServer wsServer;

    public Server() {
        var portStr = Environment.GetEnvironmentVariable("PORT");
        var debug = Environment.GetEnvironmentVariable("DEBUG") == "true";
        this.port = portStr is null ? 42069 : int.Parse(portStr);

        this.wsServer = new WebSocketServer(this.port);
        this.wsServer.AddWebSocketService<ServerConnection>("/");
        
        var logger = new LoggerConfiguration().WriteTo.Console();
        if (debug) logger = logger.MinimumLevel.Verbose();
        Logger = logger.CreateLogger();
        Log.Logger = Logger;
    }

    public void Start() {
        this.wsServer.Start();
        Log.Information("Listening on port {Port} - press any key to close", this.port);
        Console.ReadKey();
        this.wsServer.Stop();
    }

    public void TrackConnection(ServerConnection conn) {
        var player = conn.Player;
        if (player is null) {
            Log.Warning("TrackConnection but player is null? {Connection}", conn.DebugName());
            return;
        }

        // Remove from the old stage if crossing into a new one
        if (conn.LastStage != null && conn.LastStage != player.Stage) {
            this.Players[conn.LastStage.Value].Remove(conn);
            this.BroadcastNewPlayers(conn.LastStage.Value);
        }

        if (!this.Players.ContainsKey(player.Stage)) {
            this.Players[player.Stage] = new();
        }

        // Since this can be called multiple times (multiple hellos in the same
        // stage), be careful to not add it multiple times, or we'll have weird
        // state issues with multiple reported players with the same ID
        if (!this.Players[player.Stage].Contains(conn)) {
            this.Players[player.Stage].Add(conn);
        }

        this.BroadcastNewPlayers(player.Stage);
    }

    public void UntrackConnection(ServerConnection conn) {
        var player = conn.Player;

        // Don't bother untracking someone we never tracked in the first place
        if (player is null) return;

        // Contains checks just in case we get into this state somehow
        if (this.Players.ContainsKey(player.Stage)) {
            if (this.Players[player.Stage].Contains(conn)) {
                this.Players[player.Stage].Remove(conn);
            }

            this.BroadcastNewPlayers(player.Stage);
        }
    }

    private void BroadcastNewPlayers(int stage) {
        var connections = this.Players[stage].ToList();

        foreach (var connection in connections) {
            var players = connections.Select(c => c.Player)
                                     .Where(x => x?.ID != connection.Player?.ID)
                                     .ToList();

            connection.Send(new ClientboundPlayersUpdate {
                Players = players!
            });
        }
    }

    public uint GetNextID() {
        var ids = this.Connections.Select(x => x.Player!.ID).ToList();
        var id = 0u;
        while (ids.Contains(id)) id++;
        return id;
    }
}
