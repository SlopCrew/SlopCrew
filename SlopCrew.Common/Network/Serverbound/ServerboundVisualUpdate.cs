using System.IO;

namespace SlopCrew.Common.Network.Serverbound;

public class ServerboundVisualUpdate : NetworkPacket {
    public override NetworkMessageType MessageType => NetworkMessageType.ServerboundVisualUpdate;

    public int BoostpackEffect;
    public int FrictionEffect;
    public bool Spraycan;
    public bool Phone;

    public override void Read(BinaryReader br) {
        this.BoostpackEffect = br.ReadInt32();
        this.FrictionEffect = br.ReadInt32();
        this.Spraycan = br.ReadBoolean();
        this.Phone = br.ReadBoolean();
    }

    public override void Write(BinaryWriter bw) {
        bw.Write(this.BoostpackEffect);
        bw.Write(this.FrictionEffect);
        bw.Write(this.Spraycan);
        bw.Write(this.Phone);
    }
}
