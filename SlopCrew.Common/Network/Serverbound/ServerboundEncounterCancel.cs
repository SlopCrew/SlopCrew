using System.IO;

namespace SlopCrew.Common.Network.Serverbound {
    public class ServerboundEncounterCancel : NetworkPacket {
        public override NetworkMessageType MessageType => NetworkMessageType.ServerboundEncounterCancel;
        public EncounterType EncounterType;

        public override void Read(BinaryReader br) {
            this.EncounterType = (EncounterType) br.ReadInt32();
        }

        public override void Write(BinaryWriter bw) {
            bw.Write((int) this.EncounterType);
        }
    }
}
