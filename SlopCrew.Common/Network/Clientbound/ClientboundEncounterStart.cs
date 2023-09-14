using System.IO;
using SlopCrew.Common.Encounters;

namespace SlopCrew.Common.Network.Clientbound;

public class ClientboundEncounterStart : NetworkPacket {
    public override NetworkMessageType MessageType => NetworkMessageType.ClientboundEncounterStart;

    public EncounterType EncounterType;
    public EncounterConfigData EncounterConfigData;

    public override void Read(BinaryReader br) {
        this.EncounterType = (EncounterType) br.ReadInt32();
        this.EncounterConfigData = EncounterConfigData.Read(this.EncounterType, br);
    }

    public override void Write(BinaryWriter bw) {
        bw.Write((int) this.EncounterType);
        this.EncounterConfigData.Write(bw);
    }
}
