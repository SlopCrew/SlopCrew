using System;
using SlopCrew.Common.Network;
using SlopCrew.Common.Network.Clientbound;
using WebSocket = WebSocketSharp.WebSocket;

namespace SlopCrew.Plugin;

public class NetworkConnection {
    public const string Address = "192.168.1.69";
    public const uint Port = 42069;

    public event Action<NetworkPacket>? OnMessageReceived;

    private WebSocket socket;

    public NetworkConnection() {
        this.socket = new WebSocket($"ws://{Address}:{Port}");

        this.socket.OnMessage += (_, args) => {
            var packet = NetworkPacket.Read(args.RawData);
            OnMessageReceived?.Invoke(packet);
        };

        this.socket.Connect();
    }

    public void SendMessage(NetworkPacket packet) {
        this.socket.Send(packet.Serialize());
    }
}
