using System.Collections.Generic;
using System.IO;

namespace SlopCrew.Common.Network.Clientbound;

public class ClientboundServerConfig : NetworkPacket {
    public override NetworkMessageType MessageType => NetworkMessageType.ClientboundServerConfig;

    public List<string> BannedMods;

    public override void Read(BinaryReader br) {
        var count = br.ReadInt32();
        this.BannedMods = new List<string>(count);
        for (var i = 0; i < count; i++) {
            this.BannedMods.Add(br.ReadString());
        }
    }

    public override void Write(BinaryWriter bw) {
        bw.Write(this.BannedMods.Count);
        foreach (var mod in this.BannedMods) {
            bw.Write(mod);
        }
    }
}
