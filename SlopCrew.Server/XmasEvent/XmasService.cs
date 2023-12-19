using System.Threading.Channels;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.Extensions.Options;
using SlopCrew.Common.Proto;
using SlopCrew.Server.Options;

namespace SlopCrew.Server.XmasEvent;

public class XmasService : BackgroundService {

    public XmasServerEventStatePacket State;

    /// <summary>
    /// Set to true to indicate that event state has been modified and should be rebroadcast to
    /// all clients and written to disk.
    /// </summary>
    private bool stateDirty = false;

    private int stateBroadcastCooldown = 0;

    private NetworkService networkService;
    private TickRateService tickRateService;
    private ServerOptions serverOptions;
    private ILogger<XmasService> logger;
    private XmasOptions xmasOptions;

    // Runs in background, writes state to disk
    private Task storageTask;
    // Tick writes clones of state packet onto this channel.  Writer thread reads them and writes to disk
    private Channel<XmasServerEventStatePacket> storageChannel = Channel.CreateUnbounded<XmasServerEventStatePacket>();

    public XmasService(
        ILogger<XmasService> logger,
        NetworkService networkService,
        TickRateService tickRateService,
        IOptions<ServerOptions> serverOptions,
        IOptions<XmasOptions> xmasOptions
    ) {
        this.logger = logger;
        this.networkService = networkService;
        this.tickRateService = tickRateService;
        this.serverOptions = serverOptions.Value;
        this.xmasOptions = xmasOptions.Value;

        this.State = new XmasServerEventStatePacket();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) {
        this.ReadEventStateFromDiskOrCreateDefault();

        this.storageTask = Task.Run(this.StorageLoop, stoppingToken);
        
        this.tickRateService.Tick += this.Tick;

        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken) {
        this.tickRateService.Tick -= this.Tick;
        
        // Synchronously write event state to disk before shutdown
        if (this.stateDirty) {
            this.WriteEventStateToDisk();
            this.stateDirty = false;
        }

        this.storageChannel.Writer.Complete();

        return Task.Run(async () => {
            await this.storageTask.WaitAsync(cancellationToken);
        }, cancellationToken);
    }

    /// <summary>
    /// When a player collects a single gift, call this to update event state
    /// </summary>
    public void CollectGift() {
        this.stateDirty = true;
        
        // *Technically*, we could have multiple active phases at once, *if* we decided last-minute to do some hacks
        // where gifts counted towards multiple goals.
        
        for (var i = 0; i < this.State.Phases.Count; i++) {
            var phase = this.State.Phases[i];
            if (phase.Active) {
                phase.GiftsCollected++;
                if(phase.ActivateNextPhaseAutomatically && phase.GiftsCollected >= phase.GiftsGoal && i + 1 < this.State.Phases.Count) {
                    var nextPhase = this.State.Phases[i + 1];
                    phase.Active = false;
                    nextPhase.Active = true;
                    // Skip next phase in the loop, don't double-add the gift
                    i++;
                }
            }
        }
    }

    public void BroadcastEventState() {
        foreach (var stage in XmasConstants.BroadcastStateToStages) {
            this.networkService.SendToStage(stage, this.State.ToClientboundMessage());
        }
    }
    
    public void SendEventStateToPlayer(XmasClient client) {
        if(client.Client != null) {
            this.networkService.SendPacket(client.Client.Connection, this.State.ToClientboundMessage());
        }
    }
    
    public void ApplyEventStateModifications(XmasClientModifyEventStatePacket packet) {
        this.stateDirty = true;
        for (var i = 0; i < this.State.Phases.Count; i++) {
            if (i >= packet.PhaseModifications.Count) break;
            var phase = this.State.Phases[i];
            var modifications = packet.PhaseModifications[i];
            if (modifications.ModifyActive) {
                phase.Active = modifications.Phase.Active;
            }
            if (modifications.ModifyGiftsCollected) {
                phase.GiftsCollected = modifications.Phase.GiftsCollected;
            }
            if (modifications.ModifyGiftsGoal) {
                phase.GiftsGoal= modifications.Phase.GiftsGoal;
            }
            if (modifications.ModifyActivatePhaseAutomatically) {
                phase.ActivateNextPhaseAutomatically = modifications.Phase.ActivateNextPhaseAutomatically;
            }
        }
    }

    public void Tick() {
        if (this.stateBroadcastCooldown > 0) this.stateBroadcastCooldown--;
        if (this.stateDirty && this.stateBroadcastCooldown == 0) {
            this.stateBroadcastCooldown = XmasConstants.StateBroadcastCooldownInSeconds * this.serverOptions.TickRate;
            // re-broadcast and save state
            this.BroadcastEventState();
            this.WriteEventStateToDisk();
            this.stateDirty = false;
        }
    }
    
    private XmasServerEventStatePacket CreateDefaultEventState() {
        var state = new XmasServerEventStatePacket();
        for (var i = 0; i < XmasConstants.EventPhaseCount; i++) {
            state.Phases.Add(new XmasPhase() {
                ActivateNextPhaseAutomatically = true
            });
        }
        state.Phases[0].Active = true;
        state.Phases[^1].ActivateNextPhaseAutomatically = false;
        return state;
    }

    private string stateFilename() {
        return this.xmasOptions.StatePath;
    }

    private void ReadEventStateFromDiskOrCreateDefault() {
        var path = this.stateFilename();
        this.logger.LogInformation($"Reading Xmas event state from disk: {path}");
        string json;
        try {
            json = File.ReadAllText(path);
        } catch (FileNotFoundException) {
            this.logger.LogInformation("Xmas state file not found; creating default state.");
            // this is expected the first time we run the server
            this.State = this.CreateDefaultEventState();
            // Set dirty to create the file immediately
            this.stateDirty = true;
            return;
        }
        this.State = XmasEventStateSerializer.FromJson(json);
        // Set dirty so it immediately rewrites, in case code changes have changed the JSON schema 
        this.stateDirty = true;
        this.logger.LogInformation("Successfully read Xmas event state from disk");
    }

    private void WriteEventStateToDisk() {
        // Enqueue clone of state for writing to disk in worker task
        this.storageChannel.Writer.TryWrite(this.State.Clone());
    }

    private async void StorageLoop() {
        // Technically, if filesystem is slow, the channel could get backed up with queued packets.
        // Seems unlikely since we rate-limit writes w/StateBroadcastCooldownInSeconds.
        var path = this.stateFilename();
        await foreach(var p in this.storageChannel.Reader.ReadAllAsync()) {
            this.logger.LogInformation($"Writing Xmas event state to disk: {path}");
            var json = XmasEventStateSerializer.ToJson(this.State);
            File.WriteAllText(path, json);
        }
    }
}
