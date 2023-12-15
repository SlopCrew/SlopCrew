using Google.Protobuf;
using SlopCrew.Common.Proto;
using System.ComponentModel;
using System.Net.Sockets;
using System.Text;

namespace SlopCrew.Server;

public static class XmasPacketFactory {
    public static ClientboundMessage CreateAcceptGiftPacket() {
        var data = BitConverter.GetBytes((uint)1);
        return new ClientboundMessage {
            CustomPacket = new ClientboundCustomPacket {
                PlayerId = XmasConstants.ServerPlayerID,
                Packet = new CustomPacket {
                    Id = "Xmas-Server-AcceptGift",
                    Data = ByteString.CopyFrom(data)
                }
            }
        };
    }

    public static ClientboundMessage CreateRejectGiftPacket() {
        var data = BitConverter.GetBytes((uint) 1);
        return new ClientboundMessage {
            CustomPacket = new ClientboundCustomPacket {
                PlayerId = XmasConstants.ServerPlayerID,
                Packet = new CustomPacket {
                    Id = "Xmas-Server-RejectGift",
                    Data = ByteString.CopyFrom(data)
                }
            }
        };
    }
}
