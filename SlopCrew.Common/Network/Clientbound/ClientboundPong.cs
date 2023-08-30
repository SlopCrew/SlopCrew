using System.IO;

namespace SlopCrew.Common.Network.Clientbound;

public class ClientboundPong : NetworkPacket {
    public override NetworkMessageType MessageType => NetworkMessageType.ClientboundPong;

    public uint ID;

    public override void Read(BinaryReader br) {
        this.ID = br.ReadUInt32();
    }

    public override void Write(BinaryWriter bw) {
        bw.Write(this.ID);
    }
}
