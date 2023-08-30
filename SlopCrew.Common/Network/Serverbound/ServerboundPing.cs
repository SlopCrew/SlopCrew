using System.IO;

namespace SlopCrew.Common.Network.Serverbound;

public class ServerboundPing : NetworkPacket {
    public override NetworkMessageType MessageType => NetworkMessageType.ServerboundPing;

    public uint ID;

    public override void Read(BinaryReader br) {
        this.ID = br.ReadUInt32();
    }

    public override void Write(BinaryWriter bw) {
        bw.Write(this.ID);
    }
}
