using System.Collections.Generic;
using System.IO;

namespace SlopCrew.Common.Network.Serverbound;

public class ServerboundEncounterRequest : NetworkPacket {
    public override NetworkMessageType MessageType => NetworkMessageType.ServerboundEncounterRequest;

    public uint PlayerID;
    public EncounterConfig EncounterConfig;

    public override void Read(BinaryReader br) {
        this.PlayerID = br.ReadUInt32();
        var encounterConfig = new EncounterConfig();
        encounterConfig.Read(br);
        this.EncounterConfig = encounterConfig;
    }

    public override void Write(BinaryWriter bw) {
        bw.Write(this.PlayerID);
        this.EncounterConfig.Write(bw);
    }
}
