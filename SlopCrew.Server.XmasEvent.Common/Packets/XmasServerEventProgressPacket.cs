namespace SlopCrew.Server.XmasEvent;

[Serializable]
public class XmasServerEventProgressPacket : XmasPacket {
    public const string PacketId = "Xmas-Server-EventProgress";
    public override string GetPacketId() { return XmasServerEventProgressPacket.PacketId; }
    protected override uint LatestVersion => 1;

    // float from 0 to 1, 1 == completed tree
    public float TreeConstructionPercentage;

    protected override void Write(BinaryWriter writer) {
        writer.Write(TreeConstructionPercentage);
    }
    protected override void Read(BinaryReader reader) {
        switch(Version) {
            case 1:
                TreeConstructionPercentage = reader.ReadSingle();
                break;
            default:
                UnexpectedVersion();
                break;
        }
    }
}
