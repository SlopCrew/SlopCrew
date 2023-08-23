using System;
using System.IO;
using Newtonsoft.Json;
using SlopCrew.Common.Network.Clientbound;
using SlopCrew.Common.Network.Serverbound;

namespace SlopCrew.Common.Network;

public abstract class NetworkPacket : NetworkSerializable {
    public abstract NetworkMessageType MessageType { get; }

    private static MemoryStream Stream = new();
    private static BinaryWriter Writer = new(Stream);

    public static NetworkPacket Read(byte[] data) {
        //Console.WriteLine(DebugBytes("NetworkPacket.Read", data));

        using var ms = new MemoryStream(data);
        using var br = new BinaryReader(ms);

        var packetType = (NetworkMessageType) br.ReadInt32();

        NetworkPacket packet = packetType switch {
            NetworkMessageType.ClientboundPlayerAnimation => new ClientboundPlayerAnimation(),
            NetworkMessageType.ClientboundPlayerPositionUpdate => new ClientboundPlayerPositionUpdate(),
            NetworkMessageType.ClientboundPlayersUpdate => new ClientboundPlayersUpdate(),
            NetworkMessageType.ClientboundPlayerVisualUpdate => new ClientboundPlayerVisualUpdate(),

            NetworkMessageType.ServerboundAnimation => new ServerboundAnimation(),
            NetworkMessageType.ServerboundPlayerHello => new ServerboundPlayerHello(),
            NetworkMessageType.ServerboundPositionUpdate => new ServerboundPositionUpdate(),
            NetworkMessageType.ServerboundVisualUpdate => new ServerboundVisualUpdate(),
            _ => throw new Exception("dawg what")
        };

        packet.Read(br);
        return packet;
    }

    public byte[] Serialize() {
        Stream.SetLength(0);
        Writer.Seek(0, SeekOrigin.Begin);

        Writer.Write((int) this.MessageType);
        this.Write(Writer);

        var bytes = Stream.ToArray();
        //Console.WriteLine(DebugBytes("NetworkPacket.Serialize", bytes));
        return bytes;
    }

    public string DebugString() {
        return JsonConvert.SerializeObject(this);
    }

    public static string DebugBytes(string place, byte[] bytes) {
        var hex = BitConverter.ToString(bytes).Replace("-", " ");
        return $"[{place}] {hex}";
    }
}
