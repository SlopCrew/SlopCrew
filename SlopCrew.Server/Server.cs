using SlopCrew.Common.Network.Clientbound;
using WebSocketSharp.Server;

namespace SlopCrew.Server;

public class Server {
    public static Server Instance = new();

    public Dictionary<int, List<ServerConnection>> Players = new();

    private WebSocketServer wsServer;

    public Server() {
        this.wsServer = new WebSocketServer(42069);
        this.wsServer.AddWebSocketService<ServerConnection>("/");
    }

    public void Start() {
        this.wsServer.Start();
        Console.ReadKey();
        this.wsServer.Stop();
    }

    public void TrackConnection(ServerConnection conn) {
        var player = conn.Player!;

        // remove from the old stage if crossing
        if (conn.LastStage != null) {
            this.Players[conn.LastStage.Value].Remove(conn);
            this.BroadcastNewPlayers(conn.LastStage.Value);
        }

        if (!this.Players.ContainsKey(player.Stage)) {
            this.Players[player.Stage] = new();
        }

        this.Players[player.Stage].Add(conn);
        this.BroadcastNewPlayers(player.Stage);
    }

    private void BroadcastNewPlayers(int stage) {
        var connections = this.Players[stage].ToList();

        foreach (var connection in connections) {
            var players = connections.Select(c => c.Player)
                                     .Where(x => x != connection.Player)
                                     .ToList();

            connection.Send(new ClientboundPlayersUpdate {
                Players = players!
            });
        }
    }
}
