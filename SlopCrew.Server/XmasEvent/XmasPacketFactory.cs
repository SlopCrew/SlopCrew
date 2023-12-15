using Google.Protobuf;
using SlopCrew.Common.Proto;
using System.ComponentModel;
using System.Net.Sockets;
using System.Text;

namespace SlopCrew.Server.XmasEvent;

public static class XmasPacketFactory {
    public static XmasPacket? CreatePacketFromID(string id) {
        switch (id) {
            case XmasClientCollectGiftPacket.PacketId:
                return new XmasClientCollectGiftPacket();
            case XmasServerAcceptGiftPacket.PacketId:
                return new XmasServerAcceptGiftPacket();
            case XmasServerRejectGiftPacket.PacketId:
                return new XmasServerRejectGiftPacket();
        }
        return null;
    }
}
