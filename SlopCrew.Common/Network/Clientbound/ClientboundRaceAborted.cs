using System.IO;

namespace SlopCrew.Common.Network.Clientbound {
    public class ClientboundRaceAborted : NetworkPacket {
        public override NetworkMessageType MessageType => NetworkMessageType.ClientboundRaceAborted;

        public override void Read(BinaryReader br) {
        }

        public override void Write(BinaryWriter bw) {
        }
    }
}
