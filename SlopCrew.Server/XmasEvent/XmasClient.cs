using Microsoft.Extensions.Options;
using SlopCrew.Common.Proto;
using SlopCrew.Server.Options;

namespace SlopCrew.Server;

public class XmasClient : IDisposable {
    public NetworkClient? Client;
    public int CurrentGiftCooldown;
    private TickRateService tickRateService;
    private ServerOptions serverOptions;

    public XmasClient(
        TickRateService tickRateService,
        IOptions<ServerOptions> serverOptions
    ) {
        this.tickRateService = tickRateService;
        this.serverOptions = serverOptions.Value;

        this.CurrentGiftCooldown = this.serverOptions.TickRate * XmasConstants.GiftCooldownInSeconds;
        this.tickRateService.Tick += this.Tick;
    }

    // Returns true if we've handled the custom packet, to avoid it getting re-broadcasted to clients.
    public bool HandlePacket(ServerboundCustomPacket customPacket) {
        switch (customPacket.Packet.Id) {
            case "Xmas-Client-CollectGift":
                ClientboundMessage response;
                if (this.CurrentGiftCooldown > 0) {
                    response = XmasPacketFactory.CreateRejectGiftPacket();
                } else {
                    response = XmasPacketFactory.CreateAcceptGiftPacket();
                    this.CurrentGiftCooldown = this.serverOptions.TickRate * XmasConstants.GiftCooldownInSeconds;
                }
                this.Client?.SendPacket(response);
                return true;
        }
        return false;
    }

    private void Tick() {
        if (this.CurrentGiftCooldown > 0) this.CurrentGiftCooldown--;
    }

    public void Dispose() {
        this.tickRateService.Tick -= this.Tick;
    }
}
