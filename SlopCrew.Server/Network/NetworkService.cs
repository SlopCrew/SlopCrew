using System.Diagnostics;
using System.Runtime.InteropServices;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using SlopCrew.Common;
using SlopCrew.Common.Proto;
using SlopCrew.Server.Options;

namespace SlopCrew.Server;

public class NetworkService : BackgroundService {
    private ILogger<NetworkService> logger;
    private IServiceProvider provider;
    private ServerOptions serverOptions;
    private MetricsService metricsService;
    private TickRateService tickRateService;

    private NetworkingSockets? server;
    private uint pollGroup;
    private uint listenSocket;

    private Dictionary<uint, NetworkClient> clients = new();
    private Dictionary<int, List<PositionUpdate>> queuedPositionUpdates = new();
    private Dictionary<int, List<VisualUpdate>> queuedVisualUpdates = new();
    private Dictionary<int, List<AnimationUpdate>> queuedAnimationUpdates = new();
    private Queue<(uint, ServerboundMessage)> packetQueue = new();

    private CancellationTokenSource packetCts = new();
    private Task? packetTask;
    private const int MaxMessages = 256;
    private NetworkingMessage[] messages = new NetworkingMessage[MaxMessages];

    public List<NetworkClient> Clients => this.clients.Values.ToList();

    private StatusCallback callback = null!;

    public NetworkService(
        ILogger<NetworkService> logger, IServiceProvider provider,
        IOptions<ServerOptions> serverOptions,
        MetricsService metricsService, TickRateService tickRateService
    ) {
        this.logger = logger;
        this.provider = provider;
        this.serverOptions = serverOptions.Value;
        this.metricsService = metricsService;
        this.tickRateService = tickRateService;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) {
        Library.Initialize();

        this.server = new NetworkingSockets();
        this.pollGroup = this.server.CreatePollGroup();

        var utils = new NetworkingUtils();
        this.callback = this.StatusCallback;
        utils.SetStatusCallback(this.callback);

        // TODO
        var address = new Address();
        address.SetAddress("::0", this.serverOptions.Port);

        this.listenSocket = this.server.CreateListenSocket(ref address);

        this.logger.LogInformation("Now listening on port {Port}", this.serverOptions.Port);

        this.tickRateService.Tick += this.Tick;
        this.packetTask = Task.Run(async () => {
            var stopwatch = new Stopwatch();
            while (!this.packetCts.IsCancellationRequested) {
                try {
                    stopwatch.Restart();
                    this.HandleMessages();
                    var elapsed = stopwatch.ElapsedMilliseconds;
                    var delay = (1000 / this.serverOptions.TickRate) - elapsed;
                    if (delay > 0) await Task.Delay((int) delay);
                } catch (Exception e) {
                    this.logger.LogError(e, "Error while handling messages");
                }
            }
            stopwatch.Stop();
        }, this.packetCts.Token);
        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken) {
        this.tickRateService.Tick -= this.Tick;

        this.packetCts.Cancel();
        this.packetTask?.Wait(cancellationToken);

        foreach (var id in this.clients.Keys) this.server?.CloseConnection(id);
        this.clients.Clear();

        this.server?.DestroyPollGroup(this.pollGroup);
        this.server?.CloseListenSocket(this.listenSocket);
        Library.Deinitialize();

        return Task.CompletedTask;
    }

    private void QueueUpdate<T>(int stage, T update, Dictionary<int, List<T>> updates) {
        if (updates.TryGetValue(stage, out var list)) {
            list.Add(update);
        } else {
            updates.Add(stage, new() {update});
        }
    }

    private void DispatchUpdate<T>(Func<List<T>, ClientboundMessage> packetCtor, Dictionary<int, List<T>> updates) {
        foreach (var (stage, list) in updates) {
            if (list.Any()) {
                this.SendToStage(stage, packetCtor(list));
                updates[stage].Clear();
            }
        }
    }

    public void QueuePositionUpdate(int stage, PositionUpdate update) =>
        this.QueueUpdate(stage, update, this.queuedPositionUpdates);

    public void QueueVisualUpdate(int stage, VisualUpdate update) =>
        this.QueueUpdate(stage, update, this.queuedVisualUpdates);

    public void QueueAnimationUpdate(int stage, AnimationUpdate update) =>
        this.QueueUpdate(stage, update, this.queuedAnimationUpdates);

    private void Tick() {
        while (this.packetQueue.TryDequeue(out var packet)) {
            var (connection, message) = packet;
            if (this.clients.TryGetValue(connection, out var client)) client.HandlePacket(message);
        }

        this.DispatchUpdate(updates => new ClientboundMessage {
            PositionUpdate = new ClientboundPositionUpdate {
                Updates = {updates}
            }
        }, this.queuedPositionUpdates);

        this.DispatchUpdate(updates => new ClientboundMessage {
            VisualUpdate = new ClientboundVisualUpdate {
                Updates = {updates}
            }
        }, this.queuedVisualUpdates);

        this.DispatchUpdate(updates => new ClientboundMessage {
            AnimationUpdate = new ClientboundAnimationUpdate {
                Updates = {updates}
            }
        }, this.queuedAnimationUpdates);
    }

    private void HandleMessages() {
        // Loop here so we can fetch as many messages as possible (if there's over MaxMessages messages)
        while (true) {
            this.server!.RunCallbacks();
            var count = this.server.ReceiveMessagesOnPollGroup(this.pollGroup, this.messages, MaxMessages);

            if (count > 0) {
                for (var i = 0; i < count; i++) {
                    ref var netMessage = ref messages[i];
                    var data = new byte[netMessage.length];
                    Marshal.Copy(netMessage.data, data, 0, netMessage.length);

                    var packet = ServerboundMessage.Parser.ParseFrom(data);
                    if (packet is not null) this.packetQueue.Enqueue((netMessage.connection, packet));

                    netMessage.Destroy();
                }
            } else {
                break;
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
                client.Ip = info.connectionInfo.address.GetIP();
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
        this.clients.Remove(connection);
        this.metricsService.UpdateConnections(this.clients.Count);
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
