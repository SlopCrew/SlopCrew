using System.IO;

namespace SlopCrew.Common.Network.Clientbound;

public class ClientboundPlayerAnimation : NetworkPacket {
    public override NetworkMessageType MessageType => NetworkMessageType.ClientboundPlayerAnimation;

    public uint Player;
    public int Animation;
    public bool ForceOverwrite;
    public bool Instant;
    public float AtTime;

    public override void Read(BinaryReader br) {
        this.Player = br.ReadUInt32();
        this.Animation = br.ReadInt32();
        this.ForceOverwrite = br.ReadBoolean();
        this.Instant = br.ReadBoolean();
        this.AtTime = br.ReadSingle();
    }

    public override void Write(BinaryWriter bw) {
        bw.Write(this.Player);
        bw.Write(this.Animation);
        bw.Write(this.ForceOverwrite);
        bw.Write(this.Instant);
        bw.Write(this.AtTime);
    }
}
