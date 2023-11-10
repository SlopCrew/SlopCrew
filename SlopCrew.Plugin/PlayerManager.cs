using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Reptile;
using SlopCrew.Common.Proto;
using UnityEngine;

namespace SlopCrew.Plugin;

public class PlayerManager : IHostedService {
    public Dictionary<uint, AssociatedPlayer> Players = new();

    private SlopConnectionManager connectionManager;

    public PlayerManager(SlopConnectionManager connectionManager) {
        this.connectionManager = connectionManager;
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
                        Debug.Log("supposed to be removing player " + id);
                        //associatedPlayer.Dispose();
                        this.Players.Remove(id);
                    }
                }

                break;
            }

            case ClientboundMessage.MessageOneofCase.PositionUpdate: {
                foreach (var update in packet.PositionUpdate.Updates) {
                    var player = this.Players[update.PlayerId];
                    player?.ProcessPositionUpdate(update);
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
