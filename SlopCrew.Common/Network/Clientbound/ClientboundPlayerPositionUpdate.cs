using System.IO;
using System.Numerics;

namespace SlopCrew.Common.Network.Clientbound;

public class ClientboundPlayerPositionUpdate : NetworkPacket {
    public override NetworkMessageType MessageType => NetworkMessageType.ClientboundPlayerPositionUpdate;

    public uint Player;
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Velocity;
    public uint Tick;

    public override void Read(BinaryReader br) {
        this.Player = br.ReadUInt32();
        this.Position = br.ReadVector3();
        this.Rotation = br.ReadQuaternion();
        this.Velocity = br.ReadVector3();
        this.Tick = br.ReadUInt32();
    }

    public override void Write(BinaryWriter bw) {
        bw.Write(this.Player);
        bw.Write(this.Position);
        bw.Write(this.Rotation);
        bw.Write(this.Velocity);
        bw.Write(this.Tick);
    }
}
