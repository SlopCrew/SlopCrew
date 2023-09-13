using System;
using System.IO;

namespace SlopCrew.Common.Network.Serverbound;

public class ServerboundRaceFinish : NetworkPacket {
    public override NetworkMessageType MessageType => NetworkMessageType.ServerboundRaceFinish;

    public Guid Guid;
    public float Time;

    public override void Read(BinaryReader br) {
        this.Guid = new Guid(br.ReadBytes(16));
        this.Time = br.ReadSingle();
    }

    public override void Write(BinaryWriter bw) {
        bw.Write(this.Guid.ToByteArray());
        bw.Write(Time);
    }
}
