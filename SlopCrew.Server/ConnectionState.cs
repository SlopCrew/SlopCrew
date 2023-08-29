using SlopCrew.Common;
using SlopCrew.Common.Network;
using SlopCrew.Common.Network.Clientbound;
using SlopCrew.Common.Network.Serverbound;
using System.Security.Cryptography;
using System.Text;
using EmbedIO.WebSockets;
using Serilog;

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
        this.Player.Name = this.FilterPlayerName(this.Player.Name);

        var hash = SHA256.Create();
        var hashBytes = hash.ComputeHash(Encoding.UTF8.GetBytes(enter.SecretCode));
        var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        this.Player.IsDeveloper = Constants.SecretCodes.Contains(hashString);

        // Syncs player to other players
        server.TrackConnection(this);

        // Set after we track connection because State:tm:
        this.LastStage = this.Player.Stage;
    }

    private string FilterPlayerName(string name) {
        if (new ProfanityFilter.ProfanityFilter().ContainsProfanity(name.ToLower())) {
            return "Big Slopper"; // lol owned
        }

        return name[..Math.Min(32, name.Length)];
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
            if (!float.IsFinite(positionUpdate.Transform.Position[i])
                || !float.IsFinite(positionUpdate.Transform.Velocity[i])) break;
        }

        this.Player!.Transform = positionUpdate.Transform;

        this.QueuedPositionUpdate = positionUpdate.Transform;
        this.QueuedPositionUpdate.Tick = Server.CurrentTick;
    }

    private void HandleVisualUpdate(ServerboundVisualUpdate visualUpdate) {
        this.QueuedVisualUpdate = new ClientboundPlayerVisualUpdate {
            Player = this.Player!.ID,
            BoostpackEffect = visualUpdate.BoostpackEffect,
            FrictionEffect = visualUpdate.FrictionEffect,
            Spraycan = visualUpdate.Spraycan
        };
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
