using EmbedIO.WebSockets;
using Serilog;
using SlopCrew.Common.Network;

namespace SlopCrew.Server;

public class SlopWebSocketModule : WebSocketModule {
    public Dictionary<IWebSocketContext, ConnectionState> Connections = new();

    public SlopWebSocketModule() : base("/", false) { }

    protected override Task OnClientConnectedAsync(IWebSocketContext context) {
        this.Connections.Add(context, new ConnectionState(context));
        Log.Information("Now at {ConnectionCount} connections", this.Connections.Count);
        return Task.CompletedTask;
    }

    protected override Task OnClientDisconnectedAsync(IWebSocketContext context) {
        if (this.Connections.TryGetValue(context, out var state)) {
            Server.Instance.UntrackConnection(state);
        }

        this.Connections.Remove(context);

        Log.Information("Now at {ConnectionCount} connections", this.Connections.Count;
        return Task.CompletedTask;
    }

    protected override Task OnMessageReceivedAsync(
        IWebSocketContext context, byte[] buffer, IWebSocketReceiveResult result
    ) {
        try {
            var state = this.Connections[context];
            var msg = NetworkPacket.Read(buffer);
            Log.Verbose("Received message from {DebugName}: {Message}", state.DebugName(), msg.DebugString());
            state.HandlePacket(msg);
        } catch (Exception e) {
            Log.Error(e, "Error while handling message");
        }

        return Task.CompletedTask;
    }

    public void SendToContext(IWebSocketContext context, NetworkPacket msg) {
        var state = this.Connections[context];
        Log.Verbose("Sending to {DebugName}: {Message}", state.DebugName(), msg.DebugString());
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
