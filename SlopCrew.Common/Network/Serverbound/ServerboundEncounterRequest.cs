using System.IO;

namespace SlopCrew.Common.Network.Serverbound; 

public class ServerboundEncounterRequest : NetworkPacket {
    public override NetworkMessageType MessageType => NetworkMessageType.ServerboundEncounterRequest;

    public uint PlayerID;
    public Encounter.EncounterType EncounterType;
    public override void Read(BinaryReader br) {
        this.PlayerID = br.ReadUInt32();
        this.EncounterType = (Encounter.EncounterType) br.ReadInt32();
    }

    public override void Write(BinaryWriter bw) {
        bw.Write(this.PlayerID);
        bw.Write((int) this.EncounterType);
    }
}
