using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BepInEx.Logging;
using Microsoft.Extensions.Hosting;
using Reptile;
using SlopCrew.Common.Proto;

namespace SlopCrew.Plugin;

public class PlayerManager : IHostedService {
    private ManualLogSource logger;
    public Dictionary<uint, AssociatedPlayer> Players = new();

    private SlopConnectionManager connectionManager;

    public PlayerManager(SlopConnectionManager connectionManager, ManualLogSource logger) {
        this.connectionManager = connectionManager;
        this.logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken) {
        StageManager.OnStageInitialized += this.StageInit;
        this.connectionManager.MessageReceived += this.MessageReceived;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) {
        StageManager.OnStageInitialized -= this.StageInit;
        this.connectionManager.MessageReceived -= this.MessageReceived;
        return Task.CompletedTask;
    }

    public AssociatedPlayer? GetAssociatedPlayer(Reptile.Player reptilePlayer) {
        foreach (var associatedPlayer in this.Players.Values) {
            if (associatedPlayer.ReptilePlayer == reptilePlayer) return associatedPlayer;
        }

        return null;
    }

    private void MessageReceived(ClientboundMessage packet) {
        switch (packet.MessageCase) {
            case ClientboundMessage.MessageOneofCase.PlayersUpdate: {
                foreach (var player in packet.PlayersUpdate.Players) {
                    if (this.Players.TryGetValue(player.Id, out var associatedPlayer)) {
                        associatedPlayer.UpdateIfDifferent(player);
                    } else {
                        // New player
                        this.Players.Add(player.Id, new AssociatedPlayer(
                                             this.connectionManager,
                                             player));
                    }
                }

                // Remove players that are no longer here
                var packetPlayers = packet.PlayersUpdate.Players.Select(p => p.Id).ToList();
                foreach (var id in this.Players.Keys.ToList()) {
                    if (!packetPlayers.Contains(id) && this.Players.TryGetValue(id, out var associatedPlayer)) {
                        this.logger.LogDebug("supposed to be removing player " + id);
                        associatedPlayer.Dispose();
                        this.Players.Remove(id);
                    }
                }

                break;
            }

            case ClientboundMessage.MessageOneofCase.PositionUpdate: {
                foreach (var update in packet.PositionUpdate.Updates) {
                    if (this.Players.TryGetValue(update.PlayerId, out var player)) {
                        this.logger.LogDebug($"Processing position update for {update.PlayerId}");
                        player?.ProcessPositionUpdate(update);
                    }
                }

                break;
            }
        }
    }

    private void StageInit() {
        foreach (var player in this.Players.Values) player.Dispose();
        this.Players.Clear();
    }
}
