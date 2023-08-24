using System;
using System.Threading.Tasks;
using SlopCrew.Common.Network;
using WebSocket = WebSocketSharp.WebSocket;

namespace SlopCrew.Plugin;

public class NetworkConnection {
    public event Action<NetworkPacket>? OnMessageReceived;

    private WebSocket socket;

    public NetworkConnection() {
        this.socket = new WebSocket(Plugin.ConfigAddress.Value);

        this.socket.OnOpen += (_, _) => {
            if (Plugin.PlayerManager is not null) {
                Plugin.PlayerManager.IsHelloRefreshQueued = true;
            }
        };
        
        this.socket.OnMessage += (_, args) => {
            var packet = NetworkPacket.Read(args.RawData);
            // TODO move message parsing to this class
            OnMessageReceived?.Invoke(packet);
        };

        this.socket.OnClose += (_, _) => {
            Plugin.PlayerManager.IsResetQueued = true;
            Plugin.Log.LogInfo("Disconnected - reconnecting in 5s...");
            Task.Delay(5000).ContinueWith(_ => this.socket.Connect());
        };

        this.socket.Connect();
    }

    public void SendMessage(NetworkPacket packet) {
        //if (this.IsConnected) {
        var serialized = packet.Serialize();
        this.socket.Send(serialized);
        //}
    }
}
