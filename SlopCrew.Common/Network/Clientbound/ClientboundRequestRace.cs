using SlopCrew.Common.Race;
using System.IO;

namespace SlopCrew.Common.Network.Clientbound {
    public class ClientboundRequestRace : NetworkPacket {
        public override NetworkMessageType MessageType => NetworkMessageType.ClientboundRequestRace;

        public bool Response;
        public RaceConfig RaceConfig { get; set; } = new RaceConfig();
        public string InitializedTime { get; set; } = "";

        public override void Read(BinaryReader br) {
            Response = br.ReadBoolean();
            RaceConfig.Read(br);
            InitializedTime = br.ReadString();
        }

        public override void Write(BinaryWriter bw) {
            bw.Write(Response);
            RaceConfig.Write(bw);
            bw.Write(InitializedTime);
        }
    }
}
