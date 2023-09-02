using System.IO;
using SlopCrew.Common.Network;

namespace SlopCrew.Common;

public class Player : NetworkSerializable {
    public string Name;
    public uint ID;

    public int Stage;
    public int Character;
    public int Outfit;
    public int MoveStyle;

    public Transform Transform;

    public bool IsDead;
    public bool IsDeveloper;

    public override void Read(BinaryReader br) {
        this.Name = br.ReadString();
        this.ID = br.ReadUInt32();

        this.Stage = br.ReadInt32();
        this.Character = br.ReadInt32();
        this.Outfit = br.ReadInt32();
        this.MoveStyle = br.ReadInt32();

        this.Transform = new Transform();
        this.Transform.Read(br);

        this.IsDead = br.ReadBoolean();
        this.IsDeveloper = br.ReadBoolean();
    }

    public override void Write(BinaryWriter bw) {
        bw.Write(this.Name);
        bw.Write(this.ID);

        bw.Write(this.Stage);
        bw.Write(this.Character);
        bw.Write(this.Outfit);
        bw.Write(this.MoveStyle);

        this.Transform.Write(bw);

        bw.Write(this.IsDead);
        bw.Write(this.IsDeveloper);
    }
}
