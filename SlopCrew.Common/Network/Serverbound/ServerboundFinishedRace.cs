using System.IO;

namespace SlopCrew.Common.Network.Serverbound {
    public class ServerboundFinishedRace : NetworkPacket {
        public override NetworkMessageType MessageType => NetworkMessageType.ServerboundFinishedRace;

        public float Time { get; set; }

        public override void Read(BinaryReader br) {
            Time = br.ReadSingle();
        }

        public override void Write(BinaryWriter bw) {
            bw.Write(Time);
        }
    }
}
