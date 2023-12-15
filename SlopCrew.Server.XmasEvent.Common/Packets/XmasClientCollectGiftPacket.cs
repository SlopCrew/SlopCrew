
namespace SlopCrew.Server.XmasEvent;

public class XmasClientCollectGiftPacket : XmasPacket {
    public const string PacketId = "Xmas-Client-CollectGift";

    protected override uint LatestVersion => 1;

    public override string GetPacketId() { return PacketId; }

    protected override void Read(BinaryReader reader) {

    }

    protected override void Write(BinaryWriter writer) {

    }
}
