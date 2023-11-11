using System.Runtime.InteropServices;
using Google.Protobuf;
using SlopCrew.Common;
using SlopCrew.Common.Proto;
using SlopCrew.Server.Options;

namespace SlopCrew.Server;

public class NetworkService : BackgroundService {
    private ILogger<NetworkService> logger;
    private IServiceProvider provider;
    private ServerOptions options;
    private MetricsService metricsService;
    private TickRateService tickRateService;

    private NetworkingSockets? server;
    private uint pollGroup;
    private uint listenSocket;

    private Dictionary<uint, NetworkClient> clients = new();
    private Dictionary<int, List<PositionUpdate>> queuedPositionUpdates = new();

    public List<NetworkClient> Clients => this.clients.Values.ToList();

    public NetworkService(
        ILogger<NetworkService> logger, IServiceProvider provider, ServerOptions options,
        MetricsService metricsService, TickRateService tickRateService
    ) {
        this.logger = logger;
        this.provider = provider;
        this.options = options;
        this.metricsService = metricsService;
        this.tickRateService = tickRateService;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) {
        Library.Initialize();
        this.server = new NetworkingSockets();
        this.pollGroup = this.server.CreatePollGroup();

        var utils = new NetworkingUtils();
        utils.SetStatusCallback(this.StatusCallback);

        // TODO
        var address = new Address();
        address.SetAddress("::0", this.options.Port);

        this.listenSocket = this.server.CreateListenSocket(ref address);

        this.tickRateService.Tick += this.Tick;
        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken) {
        this.tickRateService.Tick -= this.Tick;

        foreach (var id in this.clients.Keys) this.server?.CloseConnection(id);
        this.clients.Clear();

        this.server?.DestroyPollGroup(this.pollGroup);
        this.server?.CloseListenSocket(this.listenSocket);
        Library.Deinitialize();

        return Task.CompletedTask;
    }

    public void QueuePositionUpdate(int stage, PositionUpdate update) {
        if (this.queuedPositionUpdates.TryGetValue(stage, out var updates)) {
            updates.Add(update);
        } else {
            this.queuedPositionUpdates.Add(stage, new() {update});
        }
    }

    private void Tick() {
        this.HandleMessages();

        foreach (var (stage, updates) in this.queuedPositionUpdates) {
            if (updates.Any()) {
                this.SendToStage(stage, new ClientboundMessage {
                    PositionUpdate = new() {
                        Updates = {updates}
                    }
                }, flags: SendFlags.Unreliable);
                this.queuedPositionUpdates[stage].Clear();
            }
        }
    }

    private void HandleMessages() {
        this.server!.RunCallbacks();

        const int maxMessages = 20;
        var messages = new NetworkingMessage[maxMessages];
        var count = this.server.ReceiveMessagesOnPollGroup(this.pollGroup, messages, maxMessages);

        if (count > 0) {
            for (var i = 0; i < count; i++) {
                ref var netMessage = ref messages[i];
                var data = new byte[netMessage.length];
                Marshal.Copy(netMessage.data, data, 0, netMessage.length);

                var packet = ServerboundMessage.Parser.ParseFrom(data);
                if (packet is not null && this.clients.TryGetValue(netMessage.connection, out var client)) {
                    client.HandlePacket(packet);
                }

                netMessage.Destroy();
            }
        }
    }

    private void StatusCallback(ref StatusInfo info) {
        switch (info.connectionInfo.state) {
            case ConnectionState.Connecting: {
                this.server!.AcceptConnection(info.connection);
                this.server!.SetConnectionPollGroup(this.pollGroup, info.connection);
                break;
            }

            case ConnectionState.Connected: {
                var client = this.provider.GetRequiredService<NetworkClient>();
                client.Connection = info.connection;
                this.clients.Add(info.connection, client);
                this.metricsService.UpdateConnections(this.clients.Count);
                break;
            }

            case ConnectionState.ClosedByPeer:
            case ConnectionState.ProblemDetectedLocally: {
                this.server!.CloseConnection(info.connection);
                if (this.clients.TryGetValue(info.connection, out var client)) {
                    client.Dispose();
                    this.clients.Remove(info.connection);
                    if (client.Stage is not null) this.BroadcastPlayersInStage(client.Stage.Value);
                    this.metricsService.UpdateConnections(this.clients.Count);
                }
                break;
            }
        }
    }

    public void Disconnect(uint connection) {
        this.server!.CloseConnection(connection);
    }

    public void SendPacket(uint connection, ClientboundMessage packet, SendFlags flags = SendFlags.Reliable) {
        var bytes = packet.ToByteArray();
        this.server!.SendMessageToConnection(connection, bytes, flags);
    }

    public void SubmitPluginVersionMetrics() {
        var pluginVersions = this.clients.Values.Select(x => x.PluginVersion)
            .Where(x => x is not null)
            .Cast<string>()
            .ToList();

        this.metricsService.SubmitPluginVersionMetrics(pluginVersions);
    }

    public uint GetNextFreeID() {
        var i = 0u;
        while (this.Clients.Any(x => x.Player?.Id == i)) i++;
        return i;
    }

    public void BroadcastPlayersInStage(int stage) {
        var players = this.Clients
            .Where(x => x.Player is not null && x.Stage == stage)
            .ToList();

        this.metricsService.UpdatePopulation(stage, players.Count);
        
        foreach (var client in players) {
            var playersWithoutClient = players
                .Select(x => x.Player!)
                .Where(x => x.Id != client.Player?.Id).ToList();

            client.SendPacket(new ClientboundMessage {
                PlayersUpdate = new() {
                    Players = {playersWithoutClient}
                }
            });
        }
    }

    public void SendToStage(
        int stage,
        ClientboundMessage packet,
        uint? exclude = null,
        SendFlags flags = SendFlags.Reliable
    ) {
        foreach (var client in this.Clients.Where(x => x.Stage == stage && x.Player?.Id != exclude)) {
            client.SendPacket(packet, flags);
        }
    }

    public void SendToTheConcerned(
        IEnumerable<uint> ids, ClientboundMessage packet, SendFlags flags = SendFlags.Reliable
    ) {
        foreach (var id in ids) {
            var client = this.Clients.FirstOrDefault(x => x.Player?.Id == id);
            client?.SendPacket(packet, flags);
        }
    }
}
