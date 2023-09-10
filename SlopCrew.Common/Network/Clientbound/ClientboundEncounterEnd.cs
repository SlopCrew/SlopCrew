using System.IO;
using SlopCrew.Common.Encounters;

namespace SlopCrew.Common.Network.Clientbound;

public class ClientboundEncounterEnd : NetworkPacket {
    public override NetworkMessageType MessageType => NetworkMessageType.ClientboundEncounterEnd;

    public EncounterType EncounterType;
    public EncounterEndData EndData;

    public override void Read(BinaryReader br) {
        this.EncounterType = (EncounterType) br.ReadInt32();
        this.EndData = EncounterEndData.Read(this.EncounterType, br);
    }

    public override void Write(BinaryWriter bw) {
        bw.Write((int) this.EncounterType);
        this.EndData.Write(bw);
    }
}
