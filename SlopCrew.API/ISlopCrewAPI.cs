using System;

namespace SlopCrew.API;

public interface ISlopCrewAPI {
    public string ServerAddress { get; }

    public int PlayerCount { get; }
    public event Action<int> OnPlayerCountChanged;

    public bool Connected { get; }
    public event Action OnConnected;
    public event Action OnDisconnected;

    public void SendCustomPacket(string id, byte[] data);
    public event Action<string, byte[]> OnCustomPacketReceived;
}
