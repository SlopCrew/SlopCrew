using System;
using SlopCrew.API;

namespace SlopCrew.Plugin;

public class SlopCrewAPI : ISlopCrewAPI {
    public string ServerAddress { get; } = string.Empty;
    public int PlayerCount { get; } = 0;
    public event Action<int>? OnPlayerCountChanged;

    public bool Connected { get; } = false;
    public event Action? OnConnected;
    public event Action? OnDisconnected;

    public int? StageOverride { get; set; }
    public void SendCustomPacket(string id, byte[] data) { }
    public event Action<string, byte[]>? OnCustomPacketReceived;
}
