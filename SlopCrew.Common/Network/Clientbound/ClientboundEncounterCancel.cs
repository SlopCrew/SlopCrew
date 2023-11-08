using System.IO;

namespace SlopCrew.Common.Network.Clientbound {
    public class ClientboundEncounterCancel : NetworkPacket {
        public override NetworkMessageType MessageType => NetworkMessageType.ClientboundEncounterCancel;
        public EncounterType EncounterType;

        public override void Read(BinaryReader br) {
            this.EncounterType = (EncounterType) br.ReadInt32();
        }

        public override void Write(BinaryWriter bw) {
            bw.Write((int) this.EncounterType);
        }
    }
}
