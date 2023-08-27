using System.IO;
using System.Numerics;
using SlopCrew.Common.Network;

namespace SlopCrew.Common;

public class Player : NetworkSerializable {
    public string Name;
    public uint ID;

    public int Stage;
    public int Character;
    public int Outfit;
    public int MoveStyle;

    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Velocity;

    public bool IsDeveloper;

    public override void Read(BinaryReader br) {
        this.Name = br.ReadString();
        this.ID = br.ReadUInt32();

        this.Stage = br.ReadInt32();
        this.Character = br.ReadInt32();
        this.Outfit = br.ReadInt32();
        this.MoveStyle = br.ReadInt32();

        this.Position = br.ReadVector3();
        this.Rotation = br.ReadQuaternion();
        this.Velocity = br.ReadVector3();
        
        this.IsDeveloper = br.ReadBoolean();
    }

    public override void Write(BinaryWriter bw) {
        bw.Write(this.Name);
        bw.Write(this.ID);

        bw.Write(this.Stage);
        bw.Write(this.Character);
        bw.Write(this.Outfit);
        bw.Write(this.MoveStyle);

        bw.Write(this.Position);
        bw.Write(this.Rotation);
        bw.Write(this.Velocity);
        
        bw.Write(this.IsDeveloper);
    }
}
