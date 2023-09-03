using SlopCrew.Common.Race;
using System.IO;

namespace SlopCrew.Common.Network.Clientbound {
    public class ClientboundRequestRace : NetworkPacket {
        public override NetworkMessageType MessageType => NetworkMessageType.ClientboundRequestRace;

        public bool Response;
        public RaceConfig RaceConfig { get; set; } = new RaceConfig();

        public override void Read(BinaryReader br) {
            Response = br.ReadBoolean();
            RaceConfig.Read(br);
        }

        public override void Write(BinaryWriter bw) {
            bw.Write(Response);
            RaceConfig.Write(bw);
        }
    }
}
