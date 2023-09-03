using System.IO;

namespace SlopCrew.Common.Network.Serverbound {
    public class ServerboundRequestRace : NetworkPacket {
        public override NetworkMessageType MessageType => NetworkMessageType.ServerboundRequestRace;

        public override void Read(BinaryReader br) {
        }

        public override void Write(BinaryWriter bw) {
        }
    }
}
