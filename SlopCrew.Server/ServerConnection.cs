using System.Text.Json;
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

    private static JsonSerializerOptions Options = new() {
        IncludeFields = true
    };

    protected override void OnMessage(MessageEventArgs e) {
        Console.WriteLine($"received message from {this.Player?.Name ?? "someone"}: " + e.Data);
        var msg = JsonSerializer.Deserialize<NetworkMessage>(e.Data, Options);
        var server = Server.Instance;

        switch (msg) {
            case ServerboundAnimation animation: {
                this.BroadcastButMe(new ClientboundPlayerAnimation {
                    Player = this.Player!.Name,
                    Animation = animation.Animation,
                    ForceOverwrite = animation.ForceOverwrite,
                    Instant = animation.Instant,
                    AtTime = animation.AtTime
                });
                break;
            }

            case ServerboundPlayerHello enter: {
                this.Player = enter.Player;
                server.TrackConnection(this);
                this.LastStage = this.Player.Stage;
                break;
            }

            case ServerboundPositionUpdate positionUpdate: {
                this.BroadcastButMe(new ClientboundPlayerPositionUpdate {
                    Player = this.Player!.Name,
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

    private string Serialize(NetworkMessage msg) {
        return JsonSerializer.Serialize(msg, Options);
    }

    public void Send(NetworkMessage msg) {
        this.Send(this.Serialize(msg));
    }

    public void BroadcastButMe(NetworkMessage msg) {
        var str = this.Serialize(msg);
        this.Sessions.Sessions
            .Where(s => s.ID != this.ID)
            .ToList()
            .ForEach(s => s.Context.WebSocket.Send(str));
    }
}
