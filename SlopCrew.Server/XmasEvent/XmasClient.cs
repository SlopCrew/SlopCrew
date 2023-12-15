using Microsoft.Extensions.Options;
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

    private void Tick() {
        if (this.CurrentGiftCooldown > 0) this.CurrentGiftCooldown--;
    }

    public void Dispose() {
        this.tickRateService.Tick -= this.Tick;
    }
}
