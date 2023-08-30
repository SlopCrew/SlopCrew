﻿using System.IO;

namespace SlopCrew.Common.Network.Serverbound;

public class ServerboundVersion : NetworkPacket {
    public override NetworkMessageType MessageType => NetworkMessageType.ServerboundVersion;

    public uint Version;

    public override void Read(BinaryReader br) {
        this.Version = br.ReadUInt32();
    }

    public override void Write(BinaryWriter bw) {
        bw.Write(this.Version);
    }
}