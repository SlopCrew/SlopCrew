using System.Data;
using SlopCrew.Common;
using SlopCrew.Common.Network;
using SlopCrew.Common.Race;

namespace SlopCrew.Server;

public enum EncounterState {
    Running,
    Finished,
}

public class StatefulEncounter {
    public Guid EncounterId;
    
    public EncounterType EncounterType;
    public EncounterState State;
    
    public List<ConnectionState> Players = new();
    
    /*
     * only replicate players who are in the same lobby
     * on disconnect ensure cleanup?
     * functions for adding and removing players
     */

    public void SendToAllPlayers(
        NetworkPacket msg
    ) {
        var ids = this.Players
            .Select(s => s.Player?.ID)
            .Where(s => s != null)
            .Select(s => s.Value);
        
        Server.Instance.Module.SendToTheConcerned(ids, msg);
    }

    public bool IsEmpty() {
        return this.Players.Count <= 0;
    }

    public StatefulEncounter(Guid encounterId) {
        this.EncounterId = encounterId;
    }
    
    public IEnumerable<uint> GetPlayersId() {
        return this.Players.Select(s => s.Player?.ID).Where(s => s != null).Select(s => s.Value);
    }

    public virtual List<NetworkPacket> Update() {
        return new();
    }
}
