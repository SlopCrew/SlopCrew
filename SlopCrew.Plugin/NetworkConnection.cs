using System;
using System.Threading.Tasks;
using SlopCrew.Common.Network;
using WebSocket = WebSocketSharp.WebSocket;

namespace SlopCrew.Plugin;


public class NetworkConnection {
    private enum SslProtocolsHack {
        Tls = 192,
        Tls11 = 768,
        Tls12 = 3072
    }

    public event Action<NetworkPacket>? OnMessageReceived;

    private WebSocket socket;


    public NetworkConnection() {
        var sslProtocolHack = (System.Security.Authentication.SslProtocols) (SslProtocolsHack.Tls12 | SslProtocolsHack.Tls11 | SslProtocolsHack.Tls);
        this.socket = new WebSocket(Plugin.ConfigAddress.Value);
        if (Plugin.ConfigAddress.Value.StartsWith("wss")) {
            this.socket.SslConfiguration.EnabledSslProtocols = sslProtocolHack;
        }
        
        this.socket.OnOpen += (_, _) => {
            Plugin.IsConnected = true;
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
            Plugin.IsConnected = false;
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
