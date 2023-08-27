using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using SlopCrew.Common.Network.Clientbound;
using SlopCrew.Common.Network.Serverbound;
using System.Text;

namespace SlopCrew.Common.Network;

public abstract class NetworkPacket : NetworkSerializable {
    public abstract NetworkMessageType MessageType { get; }

    delegate NetworkPacket PacketConstructor();
    
    static readonly Dictionary<NetworkMessageType, PacketConstructor> PacketConstructors = new Dictionary<NetworkMessageType, PacketConstructor> 
    {
        { NetworkMessageType.ClientboundPlayerAnimation, () => new ClientboundPlayerAnimation() },
        { NetworkMessageType.ClientboundPlayerPositionUpdate, () => new ClientboundPlayerPositionUpdate() },
        { NetworkMessageType.ClientboundPlayersUpdate, () => new ClientboundPlayersUpdate() },
        { NetworkMessageType.ClientboundPlayerVisualUpdate, () => new ClientboundPlayerVisualUpdate() },
        { NetworkMessageType.ClientboundSync, () => new ClientboundSync() },
        { NetworkMessageType.ServerboundAnimation, () => new ServerboundAnimation() },
        { NetworkMessageType.ServerboundPlayerHello, () => new ServerboundPlayerHello() },
        { NetworkMessageType.ServerboundPositionUpdate, () => new ServerboundPositionUpdate() },
        { NetworkMessageType.ServerboundVisualUpdate, () => new ServerboundVisualUpdate() }
    };

    public static NetworkPacket Read(byte[] data) {
        //Console.WriteLine(DebugBytes("NetworkPacket.Read", data));

        using var ms = new MemoryStream(data);
        using var br = new BinaryReader(ms);

        var packetType = (NetworkMessageType) br.ReadInt32();

        if (!PacketConstructors.TryGetValue(packetType, out var constructor)) {
            throw new Exception($"Failed to parse packet type {packetType}");
        }

        var packet = constructor();
        packet.Read(br);
        return packet;
    }

    public byte[] Serialize() 
    {
        using var ms = new MemoryStream();
        using (var bw = new BinaryWriter(ms, Encoding.Default, leaveOpen: true))
        {
            bw.Write((int) this.MessageType);
            this.Write(bw);
        }        

        if (ms.Length == ms.Capacity)
        {
          return ms.GetBuffer();
        }

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
