using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using BepInEx.Logging;
using Google.Protobuf;
using Microsoft.Extensions.Hosting;
using Reptile;
using SlopCrew.Common;
using SlopCrew.Common.Proto;
using UnityEngine;

namespace SlopCrew.Plugin;

public class ConnectionManager : IHostedService {
    public ulong ServerTick;
    public ulong Latency;

    private Config config;
    private ManualLogSource logger;
    private SlopCrewAPI api;
    private NetworkingSockets client;

    private uint? connection = null;
    private Address address;
    private ConnectionState lastState = ConnectionState.None;

    public Action<ClientboundMessage>? MessageReceived;
    public Action? Tick;
    public Action? Disconnected;

    public float? TickRate = null;
    private float tickTimer = 0;

    private Task? pingTask = null;
    private CancellationTokenSource? pingTokenSource = null;
    private DateTime lastPing = DateTime.MinValue;
    private uint pingId = 0;

    public ConnectionManager(
        Config config,
        ManualLogSource logger,
        SlopCrewAPI api
    ) {
        this.config = config;
        this.logger = logger;
        this.api = api;

        Library.Initialize();
        this.client = new NetworkingSockets();

        this.address = new Address();
        try {
            this.address.SetAddress(
                this.config.Server.Host.Value,
                this.config.Server.Port.Value
            );
        } catch {
            this.address.SetAddress(
                this.LookupIP(this.config.Server.Host.Value),
                this.config.Server.Port.Value
            );
        }

        Core.OnUpdate += this.Update;
        this.api.OnCustomPacketSent += this.SendCustomPacket;
    }

    private string LookupIP(string host) {
        try {
            var srv = Dns.GetHostEntry("_slopcrew._udp." + host);
            if (srv.AddressList.Length > 0) return srv.AddressList[0].ToString();
        } catch {
            // ignored
        }

        try {
            var a = Dns.GetHostEntry(host);
            if (a.AddressList.Length > 0) return a.AddressList[0].ToString();
        } catch {
            // ignored
        }

        throw new Exception($"Could not resolve host {host}");
    }

    public Task StartAsync(CancellationToken cancellationToken) {
        this.Connect();
        return Task.CompletedTask;
    }

    public void Connect() {
        this.connection = this.client.Connect(ref address);
        if (this.pingTask is not null) {
            this.pingTokenSource!.Cancel();
            this.pingTask.Wait();
        }

        this.pingTokenSource = new CancellationTokenSource();
        // UnityEngine.Random errors when used off the main thread
        var random = new System.Random();
        this.pingTask = Task.Run(async () => {
            while (!this.pingTokenSource!.IsCancellationRequested) {
                this.pingId = (uint) random.Next();
                this.lastPing = DateTime.Now;
                var now = (ulong) (DateTime.UtcNow.ToFileTimeUtc() / 10_000);
                this.SendMessage(new ServerboundMessage {
                    Ping = new ServerboundPing {
                        Id = this.pingId,
                        Time = now
                    }
                });

                await Task.Delay(Constants.PingFrequency, this.pingTokenSource.Token);
            }
        }, this.pingTokenSource.Token);
    }

    public Task StopAsync(CancellationToken cancellationToken) {
        Core.OnUpdate -= this.Update;

        if (this.connection is not null) this.client.CloseConnection(this.connection.Value);
        Library.Deinitialize();

        return Task.CompletedTask;
    }

    private void Update() {
        if (this.TickRate is not null) {
            this.tickTimer += Time.deltaTime;
            if (this.tickTimer >= this.TickRate) {
                this.tickTimer -= this.TickRate.Value;
                this.ServerTick++;
                this.Tick?.Invoke();
            }
        }

        this.client.RunCallbacks();
        if (this.connection == null) return;

        // The callbacks just crash. I don't know why they do.
        // This works. Free me. ~NotNite
        var info = new ConnectionInfo();
        this.client.GetConnectionInfo(this.connection.Value, ref info);
        if (info.state != this.lastState) {
            this.logger.LogDebug($"Connection state changed from {this.lastState} to {info.state}");
            this.lastState = info.state;
            this.HandleStateChange(info);
        }

        const int maxMessages = 20;
        var messages = new NetworkingMessage[maxMessages];

        var count = this.client.ReceiveMessagesOnConnection(this.connection!.Value, messages, maxMessages);
        if (count > 0) {
            for (var i = 0; i < count; i++) {
                ref var netMessage = ref messages[i];
                var data = new byte[netMessage.length];
                Marshal.Copy(netMessage.data, data, 0, netMessage.length);

                var packet = ClientboundMessage.Parser.ParseFrom(data);
                if (packet is not null) this.ProcessMessage(packet);

                netMessage.Destroy();
            }
        }
    }

    private void ProcessMessage(ClientboundMessage packet) {
        this.MessageReceived?.Invoke(packet);

        switch (packet.MessageCase) {
            case ClientboundMessage.MessageOneofCase.Hello: {
                this.TickRate = 1f / packet.Hello.TickRate;
                this.api.ChangePlayerId(packet.Hello.PlayerId);
                this.api.ChangeTickRate(packet.Hello.TickRate);
                break;
            }

            case ClientboundMessage.MessageOneofCase.Pong: {
                var latency = (uint) (DateTime.Now - this.lastPing).TotalMilliseconds;
                this.Latency = latency;
                this.ServerTick = packet.Pong.Tick;
                this.api.ChangeLatency(this.Latency);
                this.api.DispatchServerTick(ServerTick);
                break;
            }

            case ClientboundMessage.MessageOneofCase.PlayersUpdate: {
                this.api.ChangePlayerCount(packet.PlayersUpdate.Players.Count + 1);
                break;
            }

            case ClientboundMessage.MessageOneofCase.CustomPacket: {
                this.api.DispatchCustomPacket(
                    packet.CustomPacket.PlayerId,
                    packet.CustomPacket.Packet.Id,
                    packet.CustomPacket.Packet.Data.ToByteArray()
                );
                break;
            }
        }
    }

    public void SendMessage(ServerboundMessage packet, SendFlags flags = SendFlags.Reliable) {
        if (this.connection is null) throw new Exception("Not connected");
        var bytes = packet.ToByteArray();
        this.client.SendMessageToConnection(this.connection.Value, bytes, flags);
    }

    private void HandleStateChange(ConnectionInfo connectionInfo) {
        switch (connectionInfo.state) {
            case ConnectionState.Connected: {
                this.api.ChangeConnected(true);
                this.SendMessage(new ServerboundMessage {
                    Version = new ServerboundVersion {
                        ProtocolVersion = Constants.NetworkVersion,
                        PluginVersion = PluginInfo.PLUGIN_VERSION
                    }
                });
                break;
            }

            case ConnectionState.ClosedByPeer:
            case ConnectionState.ProblemDetectedLocally: {
                this.OnDisconnect();
                break;
            }
        }
    }

    private void OnDisconnect() {
        this.logger.LogWarning("Disconnected - attempting to reconnect");
        this.Disconnected?.Invoke();

        this.TickRate = null;
        this.tickTimer = 0;

        this.client.CloseConnection(this.connection!.Value);
        this.pingTokenSource?.Cancel();

        this.api.ChangeConnected(false);
        Task.Delay(Constants.ReconnectFrequency).ContinueWith(_ => this.Connect());
    }

    private void SendCustomPacket(string id, byte[] data) {
        if (data.Length > Constants.MaxCustomPacketSize) return;
        if (id.Length > Constants.MaxCustomPacketSize) return;
        
        this.SendMessage(new ServerboundMessage {
            CustomPacket = new ServerboundCustomPacket {
                Packet = new CustomPacket {
                    Id = id,
                    Data = ByteString.CopyFrom(data)
                }
            }
        });
    }
}
