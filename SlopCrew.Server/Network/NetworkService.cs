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

    private NetworkingSockets? server;
    private uint pollGroup;
    private uint listenSocket;

    private Dictionary<uint, NetworkClient> clients = new();

    private Task? recvTask;
    private CancellationTokenSource? recvCts;
    
    public List<NetworkClient> Clients => this.clients.Values.ToList();

    public NetworkService(
        ILogger<NetworkService> logger, IServiceProvider provider, ServerOptions options,
        MetricsService metricsService
    ) {
        this.logger = logger;
        this.provider = provider;
        this.options = options;
        this.metricsService = metricsService;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) {
        Library.Initialize();
        this.server = new NetworkingSockets();
        this.pollGroup = this.server.CreatePollGroup();

        var utils = new NetworkingUtils();
        utils.SetStatusCallback(this.StatusCallback);

        var address = new Address();
        address.SetAddress("::0", this.options.Port);

        this.listenSocket = this.server.CreateListenSocket(ref address);

        const int maxMessages = 20;
        var messages = new NetworkingMessage[maxMessages];

        this.recvCts = new CancellationTokenSource();
        this.recvTask = Task.Run(() => {
            while (!this.recvCts.IsCancellationRequested) {
                this.server.RunCallbacks();

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
        }, this.recvCts.Token);

        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken) {
        this.recvCts?.Cancel();
        this.recvTask?.Wait(cancellationToken);

        foreach (var id in this.clients.Keys) this.server?.CloseConnection(id);
        this.clients.Clear();

        this.server?.DestroyPollGroup(this.pollGroup);
        this.server?.CloseListenSocket(this.listenSocket);
        Library.Deinitialize();

        return Task.CompletedTask;
    }

    private void StatusCallback(ref StatusInfo info) {
        Console.WriteLine("Status callback - reason: " + info.connectionInfo.state);

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
                break;
            }

            case ConnectionState.ClosedByPeer:
            case ConnectionState.ProblemDetectedLocally: {
                this.server!.CloseConnection(info.connection);
                if (this.clients.TryGetValue(info.connection, out var client)) {
                    // todo cleanup
                    this.clients.Remove(info.connection);
                }
                break;
            }
        }
    }

    public void Disconnect(uint connection) {
        this.server!.CloseConnection(connection);
    }

    public void WritePacket(uint connection, ClientboundMessage packet, SendFlags flags = SendFlags.Reliable) {
        var bytes = packet.ToByteArray();
        Console.WriteLine($"Sending packet of type {packet.MessageCase} with {bytes.Length} bytes to {connection}");
        this.server!.SendMessageToConnection(connection, bytes, flags);
    }

    public void SubmitPluginVersionMetrics() {
        var pluginVersions = this.clients.Values.Select(x => x.PluginVersion)
            .Where(x => x is not null)
            .Cast<string>()
            .ToList();

        this.metricsService.SubmitPluginVersionMetrics(pluginVersions);
    }
}
