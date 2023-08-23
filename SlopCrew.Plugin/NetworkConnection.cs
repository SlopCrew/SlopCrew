using System;
using SlopCrew.Common.Network;
using WebSocket = WebSocketSharp.WebSocket;

namespace SlopCrew.Plugin;

public class NetworkConnection {
    public event Action<NetworkPacket>? OnMessageReceived;

    private WebSocket socket;

    public NetworkConnection() {
        this.socket = new WebSocket(Plugin.ConfigAddress.Value);

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
