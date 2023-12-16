namespace SlopCrew.Server.XmasEvent;

public static class XmasPacketFactory {

    public static XmasPacket? ParsePacket(uint playerID, string id, byte[] data) {
        var packet = CreatePacketFromID(id);
        if(packet != null) {
            packet.PlayerID = playerID;
            packet.Deserialize(data);
        }
        return packet;
    }

    public static XmasPacket? CreatePacketFromID(string id) {
        switch (id) {
            case XmasClientCollectGiftPacket.PacketId:
                return new XmasClientCollectGiftPacket();
            case XmasServerAcceptGiftPacket.PacketId:
                return new XmasServerAcceptGiftPacket();
            case XmasServerRejectGiftPacket.PacketId:
                return new XmasServerRejectGiftPacket();
            case XmasServerEventProgressPacket.PacketId:
                return new XmasServerEventProgressPacket();
        }
        return null;
    }
}
