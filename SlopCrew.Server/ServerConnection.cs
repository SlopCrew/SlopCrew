using SlopCrew.Common;
using SlopCrew.Common.Network;
using SlopCrew.Common.Network.Clientbound;
using SlopCrew.Common.Network.Serverbound;
using WebSocketSharp;
using WebSocketSharp.Server;
using ErrorEventArgs = WebSocketSharp.ErrorEventArgs;

namespace SlopCrew.Server;

public class ServerConnection : WebSocketBehavior {
    public Player? Player;
    public int? LastStage = null;

    protected override void OnMessage(MessageEventArgs e) {
        var server = Server.Instance;

        var msg = NetworkPacket.Read(e.RawData);
        Console.WriteLine($"Received message from {this.DebugName()}: " + msg.DebugString());

        if (msg is ServerboundPlayerHello enter) {
            enter.Player.ID = server.GetNextID(enter.Player.Name);
            this.Player = enter.Player;

            server.TrackConnection(this);
            this.LastStage = this.Player.Stage;
            return;
        }

        if (this.Player is null) {
            Console.WriteLine($"Received message from {this.DebugName()} without a hello, ignoring");
            // Sent message without a hello, ignore
            return;
        }

        switch (msg) {
            case ServerboundAnimation animation: {
                this.BroadcastButMe(new ClientboundPlayerAnimation {
                    Player = this.Player!.ID,
                    Animation = animation.Animation,
                    ForceOverwrite = animation.ForceOverwrite,
                    Instant = animation.Instant,
                    AtTime = animation.AtTime
                });
                break;
            }

            case ServerboundPositionUpdate positionUpdate: {
                this.BroadcastButMe(new ClientboundPlayerPositionUpdate {
                    Player = this.Player!.ID,
                    Position = positionUpdate.Position,
                    Rotation = positionUpdate.Rotation,
                    Velocity = positionUpdate.Velocity
                });
                break;
            }
        }
    }

    protected override void OnClose(CloseEventArgs e) {
        // todo
    }

    protected override void OnError(ErrorEventArgs e) {
        // todo
    }

    private byte[] Serialize(NetworkPacket msg) {
        return msg.Serialize();
    }

    public void Send(NetworkPacket msg) {
        Console.WriteLine($"Sending to {this.DebugName()}: " + msg.DebugString());
        this.Send(this.Serialize(msg));
    }

    public void BroadcastButMe(NetworkPacket msg) {
        var otherSessions = this.Sessions.Sessions
                                .Where(s => s.ID != this.ID)
                                .Cast<ServerConnection>()
                                .ToList();

        foreach (var session in otherSessions) {
            session.Send(msg);
        }
    }

    public string DebugName() {
        return this.Player != null
                   ? $"{this.Player.Name}({this.Player?.ID})"
                   : $"<player null - {this.Context.UserEndPoint}>";
    }
}
