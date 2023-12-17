using System.Runtime.Remoting.Messaging;

namespace SlopCrew.Server.XmasEvent;

[Serializable]
public class XmasServerEventStatePacket : XmasPacket {
    public const string PacketId = "Xmas-Server-EventState";
    public override string GetPacketId() { return XmasServerEventStatePacket.PacketId; }
    protected override uint LatestVersion => 1;

    public List<XmasPhase> Phases = [];
    
    protected override void Write(BinaryWriter writer) {
        writer.Write((UInt16) this.Phases.Count);
        foreach(var phase in this.Phases) {
            phase.Write(writer);
        }
    }
    protected override void Read(BinaryReader reader) {
        switch(this.Version) {
            case 1:
                this.Phases = [];
                var phaseCount = reader.ReadUInt16();
                for(var i = 0; i < phaseCount; i++) {
                    var phase = new XmasPhase();
                    phase.Read(reader);
                    this.Phases.Add(phase);
                }
                break;
            default:
                this.UnexpectedVersion();
                break;
        }
    }

    public XmasServerEventStatePacket Clone() {
        var clone = new XmasServerEventStatePacket();
        clone.PlayerID = this.PlayerID;
        clone.Version = this.Version;
        clone.Phases = new List<XmasPhase>();
        foreach(var phase in this.Phases) {
            clone.Phases.Add(phase.Clone());
        }
        return clone;
    }
}

public class XmasPhase {
    /// <summary>
    /// If true, this phase is active.  Gifts collected will count towards this phase.
    /// Only a single phase is active at once.
    /// </summary>
    public bool Active = false;

    /// <summary>
    /// Total # of gifts collected for this phase.
    /// </summary>
    public uint GiftsCollected = 0;

    /// <summary>
    /// Goal # of gifts to be collected to complete this phase of the event.
    /// </summary>
    public uint GiftsGoal = 1;

    /// <summary>
    /// If true, when this phase reaches its goal, it automatically activates the next one and deactivates itself.
    /// </summary>
    public bool ActivateNextPhaseAutomatically = false;

    public void Write(BinaryWriter writer) {
        writer.Write(this.Active);
        writer.Write((UInt16)this.GiftsCollected);
        writer.Write((UInt16)this.GiftsGoal);
        writer.Write(this.ActivateNextPhaseAutomatically);
    }

    public void Read(BinaryReader reader) {
        this.Active = reader.ReadBoolean();
        this.GiftsCollected = reader.ReadUInt16();
        this.GiftsGoal = reader.ReadUInt16();
        this.ActivateNextPhaseAutomatically = reader.ReadBoolean();
    }

    public XmasPhase Clone() {
        var clone = new XmasPhase();
        clone.Active = this.Active;
        clone.GiftsCollected = this.GiftsCollected;
        clone.GiftsGoal = this.GiftsGoal;
        clone.ActivateNextPhaseAutomatically = this.ActivateNextPhaseAutomatically;
        return clone;
    }
}
