using SlopCrew.Common;
using SlopCrew.Common.Network;
using SlopCrew.Common.Network.Clientbound;
using SlopCrew.Common.Network.Serverbound;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace SlopCrew.Server;

public class ServerConnection : WebSocketBehavior {
    public Player? Player;
    public int? LastStage = null;

    public ClientboundPlayerAnimation? QueuedAnimation;
    public ClientboundPlayerPositionUpdate? QueuedPositionUpdate;
    public ClientboundPlayerVisualUpdate? QueuedVisualUpdate;

    protected override void OnOpen() {
        Serilog.Log.Information(
            "New connection from {Connection} - now {Players} players",
            this.DebugName(),
            Server.Instance.GetConnections().Count
        );
    }

    protected override void OnMessage(MessageEventArgs e) {
        var server = Server.Instance;

        var msg = NetworkPacket.Read(e.RawData);
        Serilog.Log.Verbose("Received message from {DebugName}: {Message}", this.DebugName(), msg.DebugString());

        if (msg is ServerboundPlayerHello enter) {
            // Assign a unique ID on first hello
            // Subsequent hellos keep the originally assigned ID
            enter.Player.ID = this.Player?.ID ?? server.GetNextID();
            this.Player = enter.Player;

            // Thanks
            this.Player.Name = enter.Player.Name[..Math.Min(32, enter.Player.Name.Length)];

            // Syncs player to other players
            server.TrackConnection(this);

            // Set after we track connection because State:tm:
            this.LastStage = this.Player.Stage;
            return;
        }

        if (this.Player is null) {
            Serilog.Log.Verbose("Received message from {DebugName} without a hello, ignoring", this.DebugName());
            return;
        }

        switch (msg) {
            case ServerboundAnimation animation: {
                this.QueuedAnimation = new ClientboundPlayerAnimation {
                    Player = this.Player!.ID,
                    Animation = animation.Animation,
                    ForceOverwrite = animation.ForceOverwrite,
                    Instant = animation.Instant,
                    AtTime = animation.AtTime
                };
                break;
            }

            case ServerboundPositionUpdate positionUpdate: {
                for (var i = 0; i < 3; i++) {
                    // check to see if packet will crash clients
                    if (!float.IsFinite(positionUpdate.Position[i])
                        || !float.IsFinite(positionUpdate.Velocity[i])) break;
                }

                this.QueuedPositionUpdate = new ClientboundPlayerPositionUpdate {
                    Player = this.Player!.ID,
                    Position = positionUpdate.Position,
                    Rotation = positionUpdate.Rotation,
                    Velocity = positionUpdate.Velocity
                };
                break;
            }

            case ServerboundVisualUpdate visualUpdate: {
                this.QueuedVisualUpdate = new ClientboundPlayerVisualUpdate {
                    Player = this.Player!.ID,
                    BoostpackEffect = visualUpdate.BoostpackEffect,
                    FrictionEffect = visualUpdate.FrictionEffect,
                    Spraycan = visualUpdate.Spraycan
                };
                break;
            }
        }
    }

    protected override void OnClose(CloseEventArgs e) {
        Serilog.Log.Information("Connection closed from {Connection}: {Code} - {Reason}", this.DebugName(), e.Code,
                                e.Reason);
        Server.Instance.UntrackConnection(this);
    }

    private byte[] Serialize(NetworkPacket msg) {
        return msg.Serialize();
    }

    public void Send(NetworkPacket msg) {
        Serilog.Log.Verbose("Sending to {DebugName}: {Message}", this.DebugName(), msg.DebugString());
        this.Send(this.Serialize(msg));
    }

    public void BroadcastButMe(NetworkPacket msg) {
        var otherSessions = this.Sessions.Sessions
                                .Cast<ServerConnection>()
                                .Where(s => s.ID != this.ID)
                                .Where(s => s.Player?.Stage == this.Player?.Stage)
                                .ToList();
        var serialized = this.Serialize(msg);

        foreach (var session in otherSessions) {
            session.Send(serialized);
        }
    }

    public string DebugName() {
        var userEndPoint = "???";
        try {
            userEndPoint = this.Context.UserEndPoint.ToString();
        } catch (Exception) {
            // ignored
        }

        return this.Player != null
                   ? $"{this.Player.Name}({this.Player?.ID})"
                   : $"<player null - {userEndPoint}>";
    }

    public void RunTick() {
        if (this.QueuedAnimation is not null) {
            this.BroadcastButMe(this.QueuedAnimation);
            this.QueuedAnimation = null;
        }

        if (this.QueuedPositionUpdate is not null) {
            this.BroadcastButMe(this.QueuedPositionUpdate);
            this.QueuedPositionUpdate = null;
        }

        if (this.QueuedVisualUpdate is not null) {
            this.BroadcastButMe(this.QueuedVisualUpdate);
            this.QueuedVisualUpdate = null;
        }
    }
}
