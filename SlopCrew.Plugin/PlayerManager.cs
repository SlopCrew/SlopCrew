using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using BepInEx.Logging;
using Microsoft.Extensions.Hosting;
using Reptile;
using SlopCrew.Common;
using SlopCrew.Common.Proto;
using SlopCrew.Plugin.UI;
using UnityEngine;

namespace SlopCrew.Plugin;

public class PlayerManager(
    ConnectionManager connectionManager,
    Config config,
    CharacterInfoManager characterInfoManager,
    InterfaceUtility interfaceUtility,
    ManualLogSource logger,
    SlopCrewAPI api
)
    : IHostedService {

    public Dictionary<uint, AssociatedPlayer> Players = new();
    public List<AssociatedPlayer> AssociatedPlayers => this.Players.Values.ToList();
    public readonly InterfaceUtility InterfaceUtility = interfaceUtility;

    public bool SettingVisual;
    public bool PlayingAnimation;

    public Task StartAsync(CancellationToken cancellationToken) {
        StageManager.OnStageInitialized += this.StageInit;
        connectionManager.Disconnected += this.Disconnected;
        connectionManager.MessageReceived += this.MessageReceived;
        api.OnGetGameObjectPathForPlayerID += this.GetGameObjectPathForPlayerID;
        api.OnGetPlayerIDForGameObjectPath += this.GetPlayerIDForGameObjectPath;
        api.OnPlayerIDExists += this.PlayerIDExists;
        api.OnGetPlayerName += this.GetPlayerName;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) {
        StageManager.OnStageInitialized -= this.StageInit;
        connectionManager.Disconnected -= this.Disconnected;
        connectionManager.MessageReceived -= this.MessageReceived;
        api.OnGetGameObjectPathForPlayerID -= this.GetGameObjectPathForPlayerID;
        api.OnGetPlayerIDForGameObjectPath -= this.GetPlayerIDForGameObjectPath;
        api.OnPlayerIDExists -= this.PlayerIDExists;
        api.OnGetPlayerName -= this.GetPlayerName;
        this.CleanupPlayers();
        return Task.CompletedTask;
    }

    private string? GetGameObjectPathForPlayerID(uint playerid) {
        if (!this.Players.TryGetValue(playerid, out var associatedPlayer))
            return null;
        return associatedPlayer.ReptilePlayer.gameObject.GetPath();
    }

    private uint? GetPlayerIDForGameObjectPath(string gameObjectPath) {
        var gameObject = GameObject.Find(gameObjectPath);
        if (gameObject is null)
            return null;
        var reptilePlayer = gameObject.GetComponent<Reptile.Player>();
        if (reptilePlayer is null)
            return null;
        foreach(var player in this.Players) {
            if (player.Value.ReptilePlayer == reptilePlayer)
                return player.Key;
        }
        return null;
    }

    private bool PlayerIDExists(uint playerid) {
        if (!this.Players.TryGetValue(playerid, out var _))
            return true;
        return false;
    }

    private string? GetPlayerName(uint playerid) {
        if (!this.Players.TryGetValue(playerid, out var associatedPlayer))
            return null;
        return PlayerNameFilter.DoFilter(associatedPlayer.SlopPlayer.Name);
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
                                             this,
                                             connectionManager,
                                             config,
                                             characterInfoManager,
                                             player));
                    }
                }

                // Remove players that are no longer here
                var packetPlayers = packet.PlayersUpdate.Players.Select(p => p.Id).ToList();
                foreach (var id in this.Players.Keys.ToList()) {
                    if (!packetPlayers.Contains(id) && this.Players.TryGetValue(id, out var associatedPlayer)) {
                        associatedPlayer.Dispose();
                        this.Players.Remove(id);
                    }
                }

                break;
            }

            case ClientboundMessage.MessageOneofCase.PositionUpdate: {
                foreach (var update in packet.PositionUpdate.Updates) {
                    if (this.Players.TryGetValue(update.PlayerId, out var player)) {
                        player.QueuePositionUpdate(update);
                    }
                }

                break;
            }

            case ClientboundMessage.MessageOneofCase.VisualUpdate: {
                foreach (var update in packet.VisualUpdate.Updates) {
                    if (this.Players.TryGetValue(update.PlayerId, out var player)) {
                        player.HandleVisualUpdate(update);
                    }
                }

                break;
            }

            case ClientboundMessage.MessageOneofCase.AnimationUpdate: {
                foreach (var update in packet.AnimationUpdate.Updates) {
                    if (this.Players.TryGetValue(update.PlayerId, out var player)) {
                        player.HandleAnimationUpdate(update);
                    }
                }

                break;
            }

            case ClientboundMessage.MessageOneofCase.QuickChat: {
                if (this.Players.TryGetValue(packet.QuickChat.PlayerId, out var player)
                    && config.General.ShowQuickChat.Value) {
                    var quickChat = packet.QuickChat.QuickChat;
                    QuickChatUtility.SpawnQuickChat(player.ReptilePlayer, quickChat.Category, quickChat.Index);
                }
                break;
            }
        }
    }

    private void Disconnected() => this.CleanupPlayers();
    private void StageInit() => this.CleanupPlayers();

    private void CleanupPlayers() {
        foreach (var player in this.Players.Values) player.Dispose();
        this.Players.Clear();
    }
}
