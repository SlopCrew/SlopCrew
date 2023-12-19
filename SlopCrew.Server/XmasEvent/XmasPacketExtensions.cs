using Google.Protobuf;
using SlopCrew.Common.Proto;

namespace SlopCrew.Server.XmasEvent;

/// <summary>
/// Server-only extension methods on Xmas packets
/// </summary>
public static class XmasPacketExtensions {
    public static ClientboundMessage ToClientboundMessage(this XmasPacket packet) {
        var message = new ClientboundMessage {
            CustomPacket = new ClientboundCustomPacket {
                PlayerId = XmasConstants.ServerPlayerID,
                Packet = new CustomPacket {
                    Id = packet.GetPacketId(),
                    Data = ByteString.CopyFrom(packet.Serialize())
                }
            }
        };
        return message;
    }
}
