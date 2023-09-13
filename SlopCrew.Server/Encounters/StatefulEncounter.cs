using SlopCrew.Common;
using SlopCrew.Common.Network;

namespace SlopCrew.Server;

public enum EncounterState {
    Running,
    Finished
}

public class StatefulEncounter {
    public Guid EncounterId = Guid.NewGuid();
    public DateTime StartTime = DateTime.UtcNow;
    public EncounterType EncounterType;
    public EncounterState State;

    public List<ConnectionState> Players = new();

    public void SendToAllPlayers(NetworkPacket msg) {
        var ids = this.Players
            .Select(s => s.Player?.ID)
            .Where(s => s != null)
            .Cast<uint>();
        Server.Instance.Module.SendToTheConcerned(ids, msg);
    }

    public bool IsEmpty() {
        return this.Players.Count <= 0;
    }

    public IEnumerable<uint> GetPlayersId() {
        return this.Players.Select(s => s.Player?.ID).Where(s => s != null).Select(s => s.Value);
    }

    public virtual void Update() { }
}
