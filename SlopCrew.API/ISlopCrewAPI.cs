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

    public int? StageOverride { get; set; }
    
    public string? PlayerName { get; }

    public string? GetGameObjectPathForPlayerID(uint playerid);
    public uint? GetPlayerIDForGameObjectPath(string gameObjectPath);
    public bool? PlayerIDExists(uint playerid);
    public string? GetPlayerName(uint playerid);
    public ReadOnlyCollection<uint> Players { get; }
    public void SendCustomPacket(string id, byte[] data);
    public event Action<uint, string, byte[]> OnCustomPacketReceived;
}
