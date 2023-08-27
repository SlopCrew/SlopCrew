using System.IO;
using System.Numerics;
using SlopCrew.Common.Network;

namespace SlopCrew.Common;

public class Transform : NetworkSerializable {
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Velocity;
    public uint Tick;
    public bool Stopped;

    public override void Read(BinaryReader br) {
        this.Position = br.ReadVector3();
        this.Rotation = br.ReadQuaternion();
        this.Velocity = br.ReadVector3();
        this.Tick = br.ReadUInt32();
        this.Stopped = br.ReadBoolean();
    }

    public override void Write(BinaryWriter bw) {
        bw.Write(this.Position);
        bw.Write(this.Rotation);
        bw.Write(this.Velocity);
        bw.Write(this.Tick);
        bw.Write(this.Stopped);
    }
}
