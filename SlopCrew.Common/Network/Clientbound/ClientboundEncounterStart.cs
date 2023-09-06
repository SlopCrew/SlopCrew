using System.IO;

namespace SlopCrew.Common.Network.Clientbound;

public class ClientboundEncounterStart : NetworkPacket {
    public override NetworkMessageType MessageType => NetworkMessageType.ClientboundEncounterStart;

    public uint PlayerID;
    public EncounterConfig EncounterConfig;

    public override void Read(BinaryReader br) {
        this.PlayerID = br.ReadUInt32();
        this.EncounterConfig = EncounterConfig.ReadWithType(br);
    }

    public override void Write(BinaryWriter bw) {
        bw.Write(this.PlayerID);
        this.EncounterConfig.Write(bw);
    }
}
