using System.IO;

namespace SlopCrew.Common.Network.Serverbound {
    public class ServerboundReadyForRace : NetworkPacket {
        public override NetworkMessageType MessageType => NetworkMessageType.ServerboundReadyForRace;

        public override void Read(BinaryReader br) {
        }

        public override void Write(BinaryWriter bw) {
        }
    }
}
