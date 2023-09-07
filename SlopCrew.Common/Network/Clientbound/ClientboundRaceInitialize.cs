using System.IO;

namespace SlopCrew.Common.Network.Clientbound {
    public class ClientboundRaceInitialize : NetworkPacket {
        public override NetworkMessageType MessageType => NetworkMessageType.ClientboundRaceInitialize;

        public override void Read(BinaryReader br) {
        }

        public override void Write(BinaryWriter bw) {
        }
    }
}
