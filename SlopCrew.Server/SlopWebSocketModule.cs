using EmbedIO.WebSockets;
using Graphite;
using Serilog;
using SlopCrew.Common.Network;
using System.Collections.Concurrent;
using SlopCrew.Common;

namespace SlopCrew.Server;

public class SlopWebSocketModule : WebSocketModule {
    public ConcurrentDictionary<IWebSocketContext, ConnectionState> Connections = new();

    public SlopWebSocketModule() : base("/", true) { }

    protected override Task OnClientConnectedAsync(IWebSocketContext context) {
        this.Connections[context] = new ConnectionState(context);
        this.UpdateConnectionCount();
        return Task.CompletedTask;
    }

    protected override Task OnClientDisconnectedAsync(IWebSocketContext context) {
        if (this.Connections.TryRemove(context, out var state)) {
            Server.Instance.UntrackConnection(state);
            this.UpdateConnectionCount();
        }
        return Task.CompletedTask;
    }

    protected override Task OnMessageReceivedAsync(
        IWebSocketContext context, byte[] buffer, IWebSocketReceiveResult result
    ) {
        if (this.Connections.TryGetValue(context, out var state)) {
            try {
                var msg = NetworkPacket.Read(buffer);
                state.HandlePacket(msg);
            } catch (Exception e) {
                Log.Error(e, "Error while handling message");
            }
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
        if (!this.Connections.TryGetValue(context, out var state)) return;
        var otherSessions = this.Connections.ToList()
                                .Where(s => s.Key.Id != context.Id)
                                .Where(s => s.Value.Player?.Stage == state.Player?.Stage)
                                .ToList();

        var serialized = msg.Serialize();
        foreach (var session in otherSessions) {
            this.SendAsync(session.Key, serialized);
        }
    }

    private void UpdateConnectionCount() {
        var count = this.Connections.Count;
        Log.Information("Now at {ConnectionCount} connections", count);
        Server.Instance.Graphite?.Send("connections", count);
    }
}
