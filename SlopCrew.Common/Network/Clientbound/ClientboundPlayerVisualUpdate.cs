using System.IO;

namespace SlopCrew.Common.Network.Clientbound;

public class ClientboundPlayerVisualUpdate : NetworkPacket {
    public override NetworkMessageType MessageType => NetworkMessageType.ClientboundPlayerVisualUpdate;

    public uint Player;
    public int BoostpackEffect;
    public int FrictionEffect;
    public bool Spraycan;

    public override void Read(BinaryReader br) {
        this.Player = br.ReadUInt32();
        this.BoostpackEffect = br.ReadInt32();
        this.FrictionEffect = br.ReadInt32();
        this.Spraycan = br.ReadBoolean();
    }

    public override void Write(BinaryWriter bw) {
        bw.Write(this.Player);
        bw.Write(this.BoostpackEffect);
        bw.Write(this.FrictionEffect);
        bw.Write(this.Spraycan);
    }
}
