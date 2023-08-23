using System;
using System.Text.Json;
using SlopCrew.Common.Network;
using WebSocket = WebSocketSharp.WebSocket;

namespace SlopCrew.Plugin;

public class NetworkConnection {
    public const string Address = "192.168.1.69";
    public const uint Port = 42069;

    public event Action<NetworkMessage>? OnMessageReceived;

    private WebSocket socket;

    private static JsonSerializerOptions Options = new() {
        IncludeFields = true
    };

    public NetworkConnection() {
        this.socket = new WebSocket($"ws://{Address}:{Port}");

        this.socket.OnMessage += (_, args) => {
            Plugin.Log.LogInfo($"Received message: {args.Data}");
            var message = JsonSerializer.Deserialize<NetworkMessage>(args.Data, Options);
            if (message != null) {
                OnMessageReceived?.Invoke(message);
            }
        };

        this.socket.Connect();
    }

    public void SendMessage(NetworkMessage message) {
        var str = JsonSerializer.Serialize(message, Options);
        this.socket.Send(str);
    }
}
