using Google.Protobuf;
using Microsoft.Extensions.Options;
using SlopCrew.Common.Proto;
using SlopCrew.Server.Database;
using SlopCrew.Server.Options;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SlopCrew.Server.XmasEvent;

public class XmasClient : IDisposable {
    public bool IsSlopBrew {
        get {
            if (this.Client == null)
                return false;
            if (string.IsNullOrEmpty(this.Client.Key))
                return false;
            using var scope = this.scopeFactory.CreateScope();
            var userService = scope.ServiceProvider.GetRequiredService<UserService>();
            var user = userService.GetUserByKey(this.Client.Key);
            if (user == null)
                return false;
            if (XmasConstants.SlopBrewDiscordIDs.Contains(user.DiscordId))
                return true;
            return false;
        }
    }
    public NetworkClient? Client;
    public int CurrentGiftCooldown;
    private TickRateService tickRateService;
    private ServerOptions serverOptions;
    private IServiceScopeFactory scopeFactory;

    public XmasClient(
        TickRateService tickRateService,
        IOptions<ServerOptions> serverOptions,
        IServiceScopeFactory scopeFactory
    ) {
        this.tickRateService = tickRateService;
        this.serverOptions = serverOptions.Value;
        this.scopeFactory = scopeFactory;

        this.CurrentGiftCooldown = this.serverOptions.TickRate * XmasConstants.GiftCooldownInSeconds;
        this.tickRateService.Tick += this.Tick;
    }

    // Returns true if we've handled the custom packet, to avoid it getting re-broadcasted to clients.
    public bool HandlePacket(ServerboundCustomPacket customPacket) {
        var packet = XmasPacketFactory.CreatePacketFromID(customPacket.Packet.Id);
        if (packet == null)
            return false;
        packet.PlayerID = this.Client?.Connection;
        packet.Deserialize(customPacket.Packet.Data.ToArray());
        switch (packet.GetPacketId()) {
            case "Xmas-Client-CollectGift":
                if (this.CurrentGiftCooldown > 0) {
                    this.SendPacket(new XmasServerRejectGiftPacket());
                } else {
                    this.SendPacket(new XmasServerAcceptGiftPacket());
                    this.CurrentGiftCooldown = this.serverOptions.TickRate * XmasConstants.GiftCooldownInSeconds;
                }
                return true;
        }
        return false;
    }

    private void SendPacket(XmasPacket packet) {
        var message = new ClientboundMessage {
            CustomPacket = new ClientboundCustomPacket {
                PlayerId = XmasConstants.ServerPlayerID,
                Packet = new CustomPacket {
                    Id = packet.GetPacketId(),
                    Data = ByteString.CopyFrom(packet.Serialize())
                }
            }
        };
        this.Client?.SendPacket(message);
    }

    private void Tick() {
        if (this.CurrentGiftCooldown > 0) this.CurrentGiftCooldown--;
    }

    public void Dispose() {
        this.tickRateService.Tick -= this.Tick;
    }
}
