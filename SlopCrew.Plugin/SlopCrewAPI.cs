using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using SlopCrew.API;

namespace SlopCrew.Plugin;

public class SlopCrewAPI : ISlopCrewAPI {
    public string ServerAddress { get; internal set; } = string.Empty;
    public int PlayerCount { get; internal set; } = 0;
    public event Action<int>? OnPlayerCountChanged;

    public bool Connected { get; internal set; } = false;
    public event Action? OnConnected;
    public event Action? OnDisconnected;

    public int? StageOverride { get; set; }

    // Returns the name for the local player.
    public string? PlayerName {
        get {
            return this.OnGetLocalPlayerName?.Invoke();
        }
    }

    internal Func<string>? OnGetLocalPlayerName;

    // Unity API clients can call this then use GameObject.Find() to find the GameObject containing the Reptile.Player component for this player.
    public string? GetGameObjectPathForPlayerID(uint playerid) {
        return this.OnGetGameObjectPathForPlayerID?.Invoke(playerid);
    }

    internal event Func<uint, string?>? OnGetGameObjectPathForPlayerID;

    // Reverse of the above method; Given the path to a Player GameObject, returns the player ID.
    public uint? GetPlayerIDForGameObjectPath(string gameObjectPath) {
        return this.OnGetPlayerIDForGameObjectPath?.Invoke(gameObjectPath);
    }

    internal event Func<string, uint?>? OnGetPlayerIDForGameObjectPath;

    // Given a player ID, checks they don't exist on our end. Can be used to listen for a player disconnecting, or if a given ID is our local player.
    public bool? PlayerIDExists(uint playerid) {
        return this.OnPlayerIDExists?.Invoke(playerid);
    }

    internal event Func<uint, bool>? OnPlayerIDExists;

    // Given a player's ID, returns their name.
    public string? GetPlayerName(uint playerid) {
        return this.OnGetPlayerName?.Invoke(playerid);
    }

    internal event Func<uint, string?>? OnGetPlayerName;

    // Read-only list of all player IDs in the current stage.
    public ReadOnlyCollection<uint> Players => this.PlayersInternal.AsReadOnly();

    internal List<uint> PlayersInternal = [];

    public void SendCustomPacket(string id, byte[] data) {
        this.OnCustomPacketSent?.Invoke(id, data);
    }

    internal event Action<string, byte[]>? OnCustomPacketSent;
    public event Action<uint, string, byte[]>? OnCustomPacketReceived;

    internal void ChangeConnected(bool value) {
        if (this.Connected == value) return;
        this.Connected = value;

        if (value) {
            this.OnConnected?.Invoke();
        } else {
            this.OnDisconnected?.Invoke();
        }
    }

    internal void ChangePlayerCount(int count) {
        this.PlayerCount = count;
        this.OnPlayerCountChanged?.Invoke(count);
    }

    internal void DispatchCustomPacket(uint player, string id, byte[] data) {
        this.OnCustomPacketReceived?.Invoke(player, id, data);
    }
}
