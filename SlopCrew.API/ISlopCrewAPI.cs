using System;

namespace SlopCrew.API;

public interface ISlopCrewAPI {
    public int PlayerCount { get; }
    public string ServerAddress { get; }
    public bool Connected { get; }

    public int? StageOverride { get; set; }

    public event Action<int> OnPlayerCountChanged;
    public event Action OnConnected;
    public event Action OnDisconnected;
}
