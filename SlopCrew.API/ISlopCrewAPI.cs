using System;
using System.Collections.ObjectModel;

namespace SlopCrew.API;

public interface ISlopCrewAPI {
    public string ServerAddress { get; }

    public int PlayerCount { get; }
    public event Action<int> OnPlayerCountChanged;

    public bool Connected { get; }
    public event Action OnConnected;
    public event Action OnDisconnected;
    public ulong Latency { get; }
    public int TickRate { get; }

    public int? StageOverride { get; set; }

    public uint? PlayerId { get; }
    public string? PlayerName { get; }
    public ReadOnlyCollection<uint>? Players { get; }

    public string? GetGameObjectPathForPlayerID(uint playerId);
    public uint? GetPlayerIDForGameObjectPath(string gameObjectPath);
    public bool? PlayerIDExists(uint playerId);
    public string? GetPlayerName(uint playerId);

    public void SendCustomPacket(string id, byte[] data);
    public void SetCustomCharacterInfo(string id, byte[]? data);

    public event Action<uint, string, byte[]> OnCustomPacketReceived;
    public event Action<uint, string, byte[]> OnCustomCharacterInfoReceived;
    public event Action<ulong> OnServerTickReceived;
}
