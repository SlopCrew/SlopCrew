using System.IO;
using System.Numerics;

namespace SlopCrew.Common.Network.Serverbound;

public class ServerboundPositionUpdate : NetworkPacket {
    public override NetworkMessageType MessageType => NetworkMessageType.ServerboundPositionUpdate;

    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Velocity;

    public override void Read(BinaryReader br) {
        this.Position = br.ReadVector3();
        this.Rotation = br.ReadQuaternion();
        this.Velocity = br.ReadVector3();
    }

    public override void Write(BinaryWriter bw) {
        bw.Write(this.Position);
        bw.Write(this.Rotation);
        bw.Write(this.Velocity);
    }
}
