using System.IO;

namespace SlopCrew.Common.Network.Clientbound;

public class ClientboundEncounterStart : NetworkPacket {
    public override NetworkMessageType MessageType => NetworkMessageType.ClientboundEncounterStart;

    public uint PlayerID;
    public EncounterType EncounterType;

    public override void Read(BinaryReader br) {
        this.PlayerID = br.ReadUInt32();
        this.EncounterType = (EncounterType) br.ReadInt32();
    }

    public override void Write(BinaryWriter bw) {
        bw.Write(this.PlayerID);
        bw.Write((int) this.EncounterType);
    }
}
