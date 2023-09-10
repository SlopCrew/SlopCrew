﻿using System.IO;

namespace SlopCrew.Common.Network.Serverbound;

public class ServerboundVersion : NetworkPacket {
    public override NetworkMessageType MessageType => NetworkMessageType.ServerboundVersion;

    public uint Version;
    public string PluginVersion;

    public override void Read(BinaryReader br) {
        this.Version = br.ReadUInt32();
        this.PluginVersion = br.ReadString();
    }

    public override void Write(BinaryWriter bw) {
        bw.Write(this.Version);
        bw.Write(this.PluginVersion);
    }
}
