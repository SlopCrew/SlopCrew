using System.IO;

namespace SlopCrew.Common.Network.Clientbound;

public class ClientboundEncounterStart : NetworkPacket {
    public override NetworkMessageType MessageType => NetworkMessageType.ClientboundEncounterStart;

    public uint PlayerID;
    public EncounterType EncounterType;
    public float EncounterLength;

    public override void Read(BinaryReader br) {
        this.PlayerID = br.ReadUInt32();
        this.EncounterType = (EncounterType) br.ReadInt32();
        this.EncounterLength = br.ReadSingle();
    }

    public override void Write(BinaryWriter bw) {
        bw.Write(this.PlayerID);
        bw.Write((int) this.EncounterType);
        bw.Write(this.EncounterLength);
    }
}
