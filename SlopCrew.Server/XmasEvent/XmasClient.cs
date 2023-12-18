using Google.Protobuf;
using Microsoft.Extensions.Options;
using SlopCrew.Common.Proto;
using SlopCrew.Server.Database;
using SlopCrew.Server.Options;

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
    private XmasService xmasService;

    // Set to the ID of the current stage after we've either sent an initial event state, or determined that players on
    // this stage do not need to receive event state.
    // Every time player switches stages, we will update this.
    // Also applies when player connects.
    private int sentEventStateOnEnterStage = -1;

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
        
        using var scope = this.scopeFactory.CreateScope();
        this.xmasService = scope.ServiceProvider.GetRequiredService<XmasService>();
        
    }

    // Returns true if we've handled the custom packet, to avoid it getting re-broadcasted to clients.
    public bool HandlePacket(ServerboundCustomPacket customPacket) {
        if (this.Client == null) return false;
        var packet = XmasPacketFactory.ParsePacket(this.Client.Connection, customPacket.Packet.Id, customPacket.Packet.Data.ToArray());
        if (packet is XmasClientCollectGiftPacket collectGiftPacket) {
            this.HandleCollectGiftPacket(collectGiftPacket);
            return true;
        } else if(packet is XmasClientModifyEventStatePacket modifyPacket) {
            this.HandleModifyEventStatePacket(modifyPacket);
            return true;
        }
        return false;
    }
    
    private void HandleCollectGiftPacket(XmasClientCollectGiftPacket packet) {
        // SlopBrew members are able to ignore cooldown from debug UI, for testing
        if (this.CurrentGiftCooldown > 0 && !(packet.IgnoreCooldown && this.IsSlopBrew)) {
            this.SendPacket(new XmasServerRejectGiftPacket());
        } else {
            this.xmasService.CollectGift();
            this.SendPacket(new XmasServerAcceptGiftPacket());
            this.CurrentGiftCooldown = this.serverOptions.TickRate * XmasConstants.GiftCooldownInSeconds;
        }
    }

    private void HandleModifyEventStatePacket(XmasClientModifyEventStatePacket packet) {
        if (!this.IsSlopBrew) return;
        this.xmasService.ApplyEventStateModifications(packet);
    }

    private void SendPacket(XmasPacket packet) {
        var message = packet.ToClientboundMessage();
        this.Client?.SendPacket(message);
    }

    private void Tick() {
        if (this.CurrentGiftCooldown > 0) this.CurrentGiftCooldown--;
        if (this.Client != null && this.Client.Stage != null) {
            var stage = this.Client.Stage.Value;
            if (this.sentEventStateOnEnterStage != stage) {
                this.sentEventStateOnEnterStage = stage;
                if (XmasConstants.BroadcastStateToStages.Contains(stage)) {
                    this.xmasService.SendEventStateToPlayer(this);
                }
            }
        }
    }

    public void Dispose() {
        this.tickRateService.Tick -= this.Tick;
    }
}
