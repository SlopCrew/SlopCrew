using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SlopCrew.Common;
using SlopCrew.Common.Network;
using SlopCrew.Common.Network.Clientbound;
using SlopCrew.Common.Network.Serverbound;
using WebSocketSharp;
using WebSocket = WebSocketSharp.WebSocket;

namespace SlopCrew.Plugin;

public class NetworkConnection {
    public event Action<NetworkPacket>? OnMessageReceived;


    public uint ServerTick = 0;

    public long LastPingSent = 0;
    public uint? PingID;
    public long ServerLatency = 0;
    public Queue<long> RoundtripTimes = new();

    private Thread? tickThread;
    private Thread? pingThread;
    private WebSocket socket;

    [Flags]
    private enum SslProtocolsHack {
        Tls = 192,
        Tls11 = 768,
        Tls12 = 3072
    }

    public NetworkConnection() {
        var sslProtocolHack =
            (System.Security.Authentication.SslProtocols) (SslProtocolsHack.Tls12
                                                           | SslProtocolsHack.Tls11
                                                           | SslProtocolsHack.Tls);

        var addr = Plugin.SlopConfig.Address.Value;
        this.socket = new WebSocket(addr);
        if (addr.StartsWith("wss")) {
            this.socket.SslConfiguration.EnabledSslProtocols = sslProtocolHack;
        }

        SubscribeToSocketEvents();
        this.socket.EnableRedirection = true;
        this.socket.Connect();

        this.tickThread = new Thread(() => {
            const int tickRate = (int) (Constants.TickRate * 1000);
            while (true) {
                Thread.Sleep(tickRate);
                ServerTick++;
            }
        });
        this.tickThread.Start();

        this.pingThread = new Thread(() => {
            while (true) {
                Thread.Sleep(5000);
                if (this.PingID is not null) {
                    Plugin.Log.LogWarning("Ping took more than 5s, something is very wrong");
                }

                this.PingID = (uint) new Random().Next(0, int.MaxValue);
                this.LastPingSent = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                Plugin.NetworkConnection.SendMessage(new ServerboundPing {
                    ID = this.PingID.Value
                });
            }
        });
        this.pingThread.Start();
    }

    private void SubscribeToSocketEvents() {
        this.socket.OnOpen += OnSocketOpen;
        this.socket.OnMessage += OnSocketMessage;
        this.socket.OnClose += OnSocketClose;
        this.socket.OnError += OnSocketError;
    }

    private void OnSocketOpen(object? sender, EventArgs e) {
        Plugin.API.UpdateConnected(true);

        this.SendMessage(new ServerboundVersion {
            Version = Constants.NetworkVersion
        });

        if (Plugin.PlayerManager is not null) {
            Plugin.PlayerManager.IsHelloRefreshQueued = true;
        }
    }

    private void OnSocketMessage(object? sender, MessageEventArgs args) {
        try {
            var packet = NetworkPacket.Read(args.RawData);
            OnMessageReceived?.Invoke(packet);

            switch (packet) {
                case ClientboundPong pong:
                    this.HandlePong(pong);
                    break;

                case ClientboundSync sync:
                    this.HandleSync(sync);
                    break;
            }
        } catch (Exception e) {
            // Don't bother spamming the console with unknown packets, people don't update and it's annoying
            if (e is not Exceptions.UnknownPacketException) {
                Plugin.Log.LogError($"Error while handling packet: {e}");
            }
        }
    }

    private void OnSocketClose(object? sender, CloseEventArgs e) {
        Plugin.API.UpdateConnected(false);
        Plugin.PlayerManager.IsResetQueued = true;
        Plugin.Log.LogInfo("Disconnected - reconnecting in 5s...");
        Task.Delay(5000).ContinueWith(_ => this.socket.Connect());
    }

    private void OnSocketError(object? sender, ErrorEventArgs e) {
        Plugin.Log.LogError($"WebSocket error: {e.Message}");
        // Handle or recover from the error
    }

    public void SendMessage(NetworkPacket packet) {
        var serialized = packet.Serialize();
        this.socket.Send(serialized);
    }

    public void Dispose() {
        UnsubscribeFromSocketEvents();
        this.socket.Close();
        this.tickThread?.Abort();
        this.pingThread?.Abort();
    }

    private void UnsubscribeFromSocketEvents() {
        this.socket.OnOpen -= OnSocketOpen;
        this.socket.OnMessage -= OnSocketMessage;
        this.socket.OnClose -= OnSocketClose;
        this.socket.OnError -= OnSocketError;
    }

    private void HandlePong(ClientboundPong pong) {
        if (pong.ID != this.PingID) {
            Plugin.Log.LogWarning("Received unknown ping ID " + pong.ID);
            this.PingID = null; // huh?
            return;
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var roundtrip = now - this.LastPingSent;

        this.RoundtripTimes.Enqueue(roundtrip);
        while (this.RoundtripTimes.Count > 3) this.RoundtripTimes.Dequeue();

        this.ServerLatency = (long) this.RoundtripTimes.Average();
        this.PingID = null;
    }

    private void HandleSync(ClientboundSync sync) {
        this.ServerTick = sync.ServerTickActual;
    }
}
