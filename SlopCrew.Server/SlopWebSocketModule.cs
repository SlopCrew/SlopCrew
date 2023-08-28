using EmbedIO.WebSockets;
using Graphite;
using Serilog;
using SlopCrew.Common.Network;

namespace SlopCrew.Server;

public class SlopWebSocketModule : WebSocketModule {
    public Dictionary<IWebSocketContext, ConnectionState> Connections = new();

    public SlopWebSocketModule() : base("/", false) { }

    protected override Task OnClientConnectedAsync(IWebSocketContext context) {
        lock (this.Connections) {
            this.Connections[context] = new ConnectionState(context);
            this.UpdateConnectionCount();
        }

        return Task.CompletedTask;
    }

    protected override Task OnClientDisconnectedAsync(IWebSocketContext context) {
        lock (this.Connections) {
            if (this.Connections.TryGetValue(context, out var state)) {
                Server.Instance.UntrackConnection(state);
            }

            this.Connections.Remove(context);
            this.UpdateConnectionCount();
        }

        return Task.CompletedTask;
    }

    protected override Task OnMessageReceivedAsync(
        IWebSocketContext context, byte[] buffer, IWebSocketReceiveResult result
    ) {
        ConnectionState state;
        lock (this.Connections) {
            state = this.Connections[context];
        }

        try {
            var msg = NetworkPacket.Read(buffer);
            state.HandlePacket(msg);
        } catch (Exception e) {
            Log.Error(e, "Error while handling message");
        }

        return Task.CompletedTask;
    }

    public void SendToContext(IWebSocketContext context, NetworkPacket msg) {
        this.SendAsync(context, msg.Serialize());
    }

    public void SendToContext(IWebSocketContext context, byte[] msg) {
        this.SendAsync(context, msg);
    }

    public void BroadcastInStage(
        IWebSocketContext context,
        NetworkPacket msg
    ) {
        lock (this.Connections) {
            if (!this.Connections.TryGetValue(context, out var state)) return;

            var otherSessions = this.Connections
                                    .Where(s => s.Key.Id != context.Id)
                                    .Where(s => s.Value.Player?.Stage == state.Player?.Stage)
                                    .ToList();

            var serialized = msg.Serialize();
            foreach (var session in otherSessions) {
                this.SendAsync(session.Key, serialized);
            }
        }
    }

    private void UpdateConnectionCount() {
        var count = this.Connections.Count;
        Log.Information("Now at {ConnectionCount} connections", count);
        Server.Instance.Graphite?.Send("connections", count);
    }
}
