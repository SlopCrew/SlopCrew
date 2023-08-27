using System.IO;
using static SlopCrew.Common.Network.NetworkMessageType;

namespace SlopCrew.Common.Network.Clientbound; 

public class ClientBoundSync : NetworkPacket {
    public override NetworkMessageType MessageType => ClientboundSync;
    
    public uint ServerTickActual;

    public override void Read(BinaryReader br) {
        this.ServerTickActual = br.ReadUInt32();
    }

    public override void Write(BinaryWriter bw) {
        bw.Write(this.ServerTickActual);
    }
}
