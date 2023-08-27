using SlopCrew.Common;
using SlopCrew.Common.Network;
using SlopCrew.Common.Network.Clientbound;
using SlopCrew.Common.Network.Serverbound;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using EmbedIO.WebSockets;
using Serilog;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace SlopCrew.Server;

public class ConnectionState {
    public Player? Player;
    public int? LastStage = null;

    public ClientboundPlayerAnimation? QueuedAnimation;
    public Transform? QueuedPositionUpdate;
    public ClientboundPlayerVisualUpdate? QueuedVisualUpdate;

    public IWebSocketContext Context;

    public ConnectionState(IWebSocketContext context) {
        this.Context = context;
    }

    public void HandlePacket(NetworkPacket msg) {
        var server = Server.Instance;
        if (msg is ServerboundPlayerHello enter) {
            HandleHello(enter, server);
            return;
        }

        if (this.Player is null) {
            Log.Verbose("Received message from {DebugName} without a hello, ignoring", this.DebugName());
            return;
        }

        switch (msg) {
            case ServerboundAnimation animation:
                HandleAnimation(animation);
                break;

            case ServerboundPositionUpdate positionUpdate:
                HandlePositionUpdate(positionUpdate);
                break;

            case ServerboundVisualUpdate visualUpdate:
                HandleVisualUpdate(visualUpdate);
                break;
        }
    }

    private void HandleHello(ServerboundPlayerHello enter, Server server) {
        // Assign a unique ID on first hello
        // Subsequent hellos keep the originally assigned ID
        enter.Player.ID = this.Player?.ID ?? server.GetNextID();
        this.Player = enter.Player;

        // Thanks
        this.Player.Name = enter.Player.Name[..Math.Min(32, enter.Player.Name.Length)];

        var hash = SHA256.Create();
        var hashBytes = hash.ComputeHash(Encoding.UTF8.GetBytes(enter.SecretCode));
        var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        this.Player.IsDeveloper = Constants.SecretCodes.Contains(hashString);

        // Syncs player to other players
        server.TrackConnection(this);

        // Set after we track connection because State:tm:
        this.LastStage = this.Player.Stage;
    }

    private void HandleAnimation(ServerboundAnimation animation) {
        this.QueuedAnimation = new ClientboundPlayerAnimation {
            Player = this.Player!.ID,
            Animation = animation.Animation,
            ForceOverwrite = animation.ForceOverwrite,
            Instant = animation.Instant,
            AtTime = animation.AtTime
        };
    }

    private void HandlePositionUpdate(ServerboundPositionUpdate positionUpdate) {
        for (var i = 0; i < 3; i++) {
            // check to see if packet will crash clients
            if (!float.IsFinite(positionUpdate.Position[i])
                || !float.IsFinite(positionUpdate.Velocity[i])) break;
        }

        this.Player!.Position = positionUpdate.Position;
        this.Player.Rotation = positionUpdate.Rotation;
        this.Player.Velocity = positionUpdate.Velocity;

        this.QueuedPositionUpdate = new Transform {
            Position = positionUpdate.Position,
            Rotation = positionUpdate.Rotation,
            Velocity = positionUpdate.Velocity
        };
    }

    private void HandleVisualUpdate(ServerboundVisualUpdate visualUpdate) {
        this.QueuedVisualUpdate = new ClientboundPlayerVisualUpdate {
            Player = this.Player!.ID,
            BoostpackEffect = visualUpdate.BoostpackEffect,
            FrictionEffect = visualUpdate.FrictionEffect,
            Spraycan = visualUpdate.Spraycan
        };
    }

    protected void OnClose(CloseEventArgs e) {
        Log.Information("Connection closed from {Connection}: {Code} - {Reason}", this.DebugName(), e.Code,
                        e.Reason);
        Server.Instance.UntrackConnection(this);
    }


    public string DebugName() {
        var endpoint = this.Context.RemoteEndPoint.ToString();

        return this.Player != null
                   ? $"{this.Player.Name}({this.Player?.ID})"
                   : $"<player null - {endpoint}>";
    }

    public void RunTick() {
        var module = Server.Instance.Module;

        if (this.QueuedAnimation is not null) {
            module.BroadcastInStage(this.Context, this.QueuedAnimation);
            this.QueuedAnimation = null;
        }

        if (this.QueuedVisualUpdate is not null) {
            module.BroadcastInStage(this.Context, this.QueuedVisualUpdate);
            this.QueuedVisualUpdate = null;
        }
    }
}
