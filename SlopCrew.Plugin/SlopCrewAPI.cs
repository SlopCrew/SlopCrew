using System;
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
    public ulong Latency { get; internal set; } = 0;
    public int TickRate { get; internal set; } = 0;

    public int? StageOverride { get; set; }
    
    // Returns the id for the local player.
    public uint? PlayerId { get; private set; }
    // Returns the name for the local player.
    public string? PlayerName {
        get { return this.OnGetLocalPlayerName?.Invoke(); }
    }

    internal Func<string>? OnGetLocalPlayerName;

    // Unity API clients can call this then use GameObject.Find() to find the GameObject containing the Reptile.Player component for this player.
    public string? GetGameObjectPathForPlayerID(uint playerId) {
        return this.OnGetGameObjectPathForPlayerID?.Invoke(playerId);
    }

    internal event Func<uint, string?>? OnGetGameObjectPathForPlayerID;

    // Reverse of the above method; Given the path to a Player GameObject, returns the player ID.
    public uint? GetPlayerIDForGameObjectPath(string gameObjectPath) {
        return this.OnGetPlayerIDForGameObjectPath?.Invoke(gameObjectPath);
    }

    internal event Func<string, uint?>? OnGetPlayerIDForGameObjectPath;

    // Given a player ID, checks they don't exist on our end. Can be used to listen for a player disconnecting, or if a given ID is our local player.
    public bool? PlayerIDExists(uint playerId) {
        return this.OnPlayerIDExists?.Invoke(playerId);
    }

    internal event Func<uint, bool>? OnPlayerIDExists;

    // Given a player's ID, returns their name.
    public string? GetPlayerName(uint playerId) {
        return this.OnGetPlayerName?.Invoke(playerId);
    }

    internal event Func<uint, string?>? OnGetPlayerName;

    // Read-only list of all player IDs in the current stage.
    public ReadOnlyCollection<uint>? Players => this.OnGetPlayerList?.Invoke();

    internal event Func<ReadOnlyCollection<uint>>? OnGetPlayerList;

    public void SendCustomPacket(string id, byte[] data) {
        this.OnCustomPacketSent?.Invoke(id, data);
    }

    public void SetCustomCharacterInfo(string id, byte[]? data) {
        this.OnCustomCharacterInfoSet?.Invoke(id, data);
    }

    internal event Action<string, byte[]>? OnCustomPacketSent;
    internal event Action<string, byte[]?>? OnCustomCharacterInfoSet;

    public event Action<uint, string, byte[]>? OnCustomPacketReceived;
    public event Action<uint, string, byte[]>? OnCustomCharacterInfoReceived;
    public event Action<ulong>? OnServerTickReceived;

    internal void ChangeConnected(bool value) {
        if (this.Connected == value) return;
        this.Connected = value;

        if (value) {
            this.OnConnected?.Invoke();
        } else {
            this.OnDisconnected?.Invoke();
        }
    }
    
    internal void ChangePlayerId(uint playerId) {
        this.PlayerId = playerId;
    }
    internal void ChangeLatency(ulong latency) {
        this.Latency = latency;
    }
    
    internal void ChangeTickRate(int tickRate) {
        this.TickRate = tickRate;
    }

    internal void ChangePlayerCount(int count) {
        this.PlayerCount = count;
        this.OnPlayerCountChanged?.Invoke(count);
    }

    internal void DispatchCustomPacket(uint player, string id, byte[] data) {
        this.OnCustomPacketReceived?.Invoke(player, id, data);
    }
    
    internal void DispatchServerTick(ulong tick) {
        this.OnServerTickReceived?.Invoke(tick);
    }
}
