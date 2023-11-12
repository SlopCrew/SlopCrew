using SlopCrew.Common.Proto;

namespace SlopCrew.Server.Encounters;

public abstract class Encounter : IDisposable {
    public List<NetworkClient> Clients = new();
    public EncounterType Type;
    public int Stage;
    public bool Finished;

    public virtual void Update() {
        // Filter disconnected players
        this.Clients = this.Clients.Where(x => x.Stage == this.Stage && x.IsConnected()).ToList();
    }

    public abstract void ProcessPacket(NetworkClient client, ServerboundEncounterUpdate packet);
    public abstract void Dispose();
}
