
namespace SlopCrew.Server.XmasEvent;

[Serializable]
public class XmasServerAcceptGiftPacket : XmasPacket {
    public const string PacketId = "Xmas-Server-AcceptGift";

    protected override uint LatestVersion => 1;

    public override string GetPacketId() { return PacketId; }

    protected override void Read(BinaryReader reader) {

    }

    protected override void Write(BinaryWriter writer) {

    }
}
