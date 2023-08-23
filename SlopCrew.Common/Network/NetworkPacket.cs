using System;
using System.IO;
using Newtonsoft.Json;
using SlopCrew.Common.Network.Clientbound;
using SlopCrew.Common.Network.Serverbound;

namespace SlopCrew.Common.Network;

public abstract class NetworkPacket : NetworkSerializable {
    public abstract NetworkMessageType MessageType { get; }

    public static NetworkPacket Read(byte[] data) {
        //Console.WriteLine(DebugBytes("NetworkPacket.Read", data));

        using var ms = new MemoryStream(data);
        using var br = new BinaryReader(ms);

        var packetType = (NetworkMessageType) br.ReadInt32();

        NetworkPacket packet = packetType switch {
            NetworkMessageType.ClientboundPlayerAnimation => new ClientboundPlayerAnimation(),
            NetworkMessageType.ClientboundPlayerPositionUpdate => new ClientboundPlayerPositionUpdate(),
            NetworkMessageType.ClientboundPlayersUpdate => new ClientboundPlayersUpdate(),
            NetworkMessageType.ServerboundAnimation => new ServerboundAnimation(),
            NetworkMessageType.ServerboundPlayerHello => new ServerboundPlayerHello(),
            NetworkMessageType.ServerboundPositionUpdate => new ServerboundPositionUpdate(),
            _ => throw new Exception("dawg what")
        };

        packet.Read(br);
        return packet;
    }

    public byte[] Serialize() {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        bw.Write((int) this.MessageType);
        this.Write(bw);

        var bytes = ms.ToArray();
        //Console.WriteLine(DebugBytes("NetworkPacket.Serialize", bytes));

        return ms.ToArray();
    }

    public string DebugString() {
        return JsonConvert.SerializeObject(this);
    }

    public static string DebugBytes(string place, byte[] bytes) {
        var hex = BitConverter.ToString(bytes).Replace("-", " ");
        return $"[{place}] {hex}";
    }
}
