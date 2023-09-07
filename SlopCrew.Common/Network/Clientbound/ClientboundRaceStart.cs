using System.IO;

namespace SlopCrew.Common.Network.Clientbound {
    public class ClientboundRaceStart : NetworkPacket {
        public override NetworkMessageType MessageType => NetworkMessageType.ClientboundRaceStart;

        public override void Read(BinaryReader br) {
        }

        public override void Write(BinaryWriter bw) {
        }
    }
}
