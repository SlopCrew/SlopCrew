using System.IO;

namespace SlopCrew.Common.Network.Serverbound;

public class ServerboundAnimation : NetworkPacket {
    public override NetworkMessageType MessageType => NetworkMessageType.ServerboundAnimation;
    
    public int Animation;
    public bool ForceOverwrite;
    public bool Instant;
    public float AtTime;

    public override void Read(BinaryReader br) {
        this.Animation = br.ReadInt32();
        this.ForceOverwrite = br.ReadBoolean();
        this.Instant = br.ReadBoolean();
        this.AtTime = br.ReadSingle();
    }

    public override void Write(BinaryWriter bw) {
        bw.Write(this.Animation);
        bw.Write(this.ForceOverwrite);
        bw.Write(this.Instant);
        bw.Write(this.AtTime);
    }
}
