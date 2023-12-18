using System;
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
