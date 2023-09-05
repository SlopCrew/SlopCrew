using System.IO;

namespace SlopCrew.Common.Network.Serverbound;

public class ServerboundVisualUpdate : NetworkPacket {
    public override NetworkMessageType MessageType => NetworkMessageType.ServerboundVisualUpdate;

    public int BoostpackEffect;
    public int FrictionEffect;
    public bool Spraycan;
    public bool Phone;
    public int SpraycanState;

    public override void Read(BinaryReader br) {
        this.BoostpackEffect = br.ReadInt32();
        this.FrictionEffect = br.ReadInt32();
        this.Spraycan = br.ReadBoolean();
        this.Phone = br.ReadBoolean();
        // backwards compat
        if (br.BaseStream.Position != br.BaseStream.Length) {
            // This doesnt need to be a whole int32 but thats how `enum`s are represented in the CLR ~Sylvie
            this.SpraycanState = br.ReadInt32();
        }
    }

    public override void Write(BinaryWriter bw) {
        bw.Write(this.BoostpackEffect);
        bw.Write(this.FrictionEffect);
        bw.Write(this.Spraycan);
        bw.Write(this.Phone);
        bw.Write(this.SpraycanState);
    }
}
