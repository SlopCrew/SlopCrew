using System.Collections.Generic;
using System.IO;

namespace SlopCrew.Common.Network.Clientbound;

public class ClientboundPlayerPositionUpdate : NetworkPacket {
    public override NetworkMessageType MessageType => NetworkMessageType.ClientboundPlayerPositionUpdate;

    public Dictionary<uint, Transform> Positions;
    public uint Tick;

    public override void Read(BinaryReader br) {
        var len = br.ReadInt32();
        this.Positions = new Dictionary<uint, Transform>(len);
        for (var i = 0; i < len; i++) {
            var player = br.ReadUInt32();
            var position = new Transform();
            position.Read(br);
            this.Positions.Add(player, position);
        }
        this.Tick = br.ReadUInt32();
    }

    public override void Write(BinaryWriter bw) {
        bw.Write(this.Positions.Count);
        foreach (var kvp in this.Positions) {
            bw.Write(kvp.Key);
            kvp.Value.Write(bw);
        }
        bw.Write(this.Stopped);
    }
}
