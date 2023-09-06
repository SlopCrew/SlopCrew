using System.IO;

namespace SlopCrew.Common.Network.Clientbound;

// TODO merge into EncounterStart someday
public class ClientboundEncounterRequest : NetworkPacket {
    public override NetworkMessageType MessageType => NetworkMessageType.ClientboundEncounterRequest;

    public uint PlayerID;
    public EncounterConfig EncounterConfig;

    public override void Read(BinaryReader br) {
        this.PlayerID = br.ReadUInt32();
        var encounterConfig = new EncounterConfig();
        encounterConfig.Read(br);
        this.EncounterConfig = encounterConfig;
    }

    public override void Write(BinaryWriter bw) {
        bw.Write(this.PlayerID);
        this.EncounterConfig.Write(bw);
    }
}
