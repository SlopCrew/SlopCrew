using System.IO;

namespace SlopCrew.Common.Network.Clientbound;

// TODO merge into EncounterStart someday
public class ClientboundEncounterRequest : NetworkPacket {
    public override NetworkMessageType MessageType => NetworkMessageType.ClientboundEncounterRequest;

    public uint PlayerID;

    public override void Read(BinaryReader br) {
        this.PlayerID = br.ReadUInt32();
    }

    public override void Write(BinaryWriter bw) {
        bw.Write(this.PlayerID);
    }
}
