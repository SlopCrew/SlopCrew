using System.IO;
using static SlopCrew.Common.Network.NetworkMessageType;

namespace SlopCrew.Common.Network.Clientbound; 

public class ClientboundSync : NetworkPacket {
    public override NetworkMessageType MessageType => NetworkMessageType.ClientboundSync;
    
    public uint ServerTickActual;

    public override void Read(BinaryReader br) {
        this.ServerTickActual = br.ReadUInt32();
    }

    public override void Write(BinaryWriter bw) {
        bw.Write(this.ServerTickActual);
    }
}
