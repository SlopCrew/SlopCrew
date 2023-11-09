using System.IO;

namespace SlopCrew.Common.Network.Serverbound;

public class ServerboundVersion : NetworkPacket {
    public override NetworkMessageType MessageType => NetworkMessageType.ServerboundVersion;

    public uint Version;
    public string? PluginVersion;

    public override void Read(BinaryReader br) {
        this.Version = br.ReadUInt32();
        if (br.BaseStream.Position < br.BaseStream.Length) this.PluginVersion = br.ReadString();
    }

    public override void Write(BinaryWriter bw) {
        bw.Write(this.Version);
        if (this.PluginVersion != null) bw.Write(this.PluginVersion);
    }
}
