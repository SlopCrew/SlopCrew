using SlopCrew.Common;
using SlopCrew.Common.Proto;

namespace SlopCrew.Server;

public class NetworkClient {
    public string? PluginVersion;
    public uint Connection;
    private NetworkService networkService;

    public NetworkClient(NetworkService networkService) {
        this.networkService = networkService;
    }

    public void HandlePacket(ServerboundMessage packet) {
        switch (packet.MessageCase) {
            case ServerboundMessage.MessageOneofCase.Version: {
                if (packet.Version.ProtocolVersion != Constants.NetworkVersion) {
                    this.Disconnect();
                    return;
                }

                this.PluginVersion = packet.Version.PluginVersion;
                this.networkService.SubmitPluginVersionMetrics();
                
                this.SendPacket(new ClientboundMessage {
                    Hello = new ClientboundHello {
                        // TODO config this
                        TickRate = 10,
                        BannedPlugins = { }
                    }
                });

                break;
            }
        }
    }

    public void Disconnect() => this.networkService.Disconnect(this.Connection);

    public void SendPacket(ClientboundMessage packet, SendFlags flags = SendFlags.Reliable) =>
        this.networkService.WritePacket(this.Connection, packet, flags);
}
