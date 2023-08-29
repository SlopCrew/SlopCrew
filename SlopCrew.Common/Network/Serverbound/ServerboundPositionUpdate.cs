using System.IO;
using System.Numerics;

namespace SlopCrew.Common.Network.Serverbound;

public class ServerboundPositionUpdate : NetworkPacket {
    public override NetworkMessageType MessageType => NetworkMessageType.ServerboundPositionUpdate;

    public Transform Transform;

    public override void Read(BinaryReader br) {
        this.Transform = new Transform();
        this.Transform.Read(br);
    }

    public override void Write(BinaryWriter bw) {
        this.Transform.Write(bw);
    }
}
