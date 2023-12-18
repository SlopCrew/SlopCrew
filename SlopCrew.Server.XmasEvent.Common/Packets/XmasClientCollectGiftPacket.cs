
using System.Diagnostics;

namespace SlopCrew.Server.XmasEvent;

[Serializable]
public class XmasClientCollectGiftPacket : XmasPacket {
    public const string PacketId = "Xmas-Client-CollectGift";

    protected override uint LatestVersion => 1;

    public bool IgnoreCooldown = false;

    public override string GetPacketId() { return PacketId; }

    protected override void Read(BinaryReader reader) {
        this.IgnoreCooldown = reader.ReadBoolean();
    }

    protected override void Write(BinaryWriter writer) {
        writer.Write(this.IgnoreCooldown);
    }

    public override string Describe() {
        return base.Describe() + $" IgnoreCooldown={this.IgnoreCooldown}";
    }
}
