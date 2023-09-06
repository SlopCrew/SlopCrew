using SlopCrew.Common.Network.Clientbound;
using SlopCrew.Common.Network.Serverbound;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SlopCrew.Common.Network;

public abstract class NetworkPacket : NetworkSerializable {
    public abstract NetworkMessageType MessageType { get; }

    delegate NetworkPacket PacketConstructor();

    // TODO(1.5.0): do this with reflection
    static readonly Dictionary<NetworkMessageType, PacketConstructor> PacketConstructors =
        new() {
            {NetworkMessageType.ServerboundVersion, () => new ServerboundVersion()},

            {NetworkMessageType.ClientboundPlayerAnimation, () => new ClientboundPlayerAnimation()},
            {NetworkMessageType.ClientboundPlayerPositionUpdate, () => new ClientboundPlayerPositionUpdate()},
            {NetworkMessageType.ClientboundPlayerScoreUpdate, () => new ClientboundPlayerScoreUpdate()},
            {NetworkMessageType.ClientboundPlayersUpdate, () => new ClientboundPlayersUpdate()},
            {NetworkMessageType.ClientboundPlayerVisualUpdate, () => new ClientboundPlayerVisualUpdate()},
            {NetworkMessageType.ClientboundPong, () => new ClientboundPong()},
            {NetworkMessageType.ClientboundSync, () => new ClientboundSync()},
            {NetworkMessageType.ClientboundEncounterRequest, () => new ClientboundEncounterRequest()},
            {NetworkMessageType.ClientboundEncounterStart, () => new ClientboundEncounterStart()},
            {NetworkMessageType.ClientboundRequestRace, () => new ClientboundRequestRace()},
            {NetworkMessageType.ClientboundRaceInitialize, () => new ClientboundRaceInitialize()},
            {NetworkMessageType.ClientboundRaceStart, () => new ClientboundRaceStart()},
            {NetworkMessageType.ClientboundRaceRank, () => new ClientboundRaceRank()},
            {NetworkMessageType.ClientboundRaceAborted, () => new ClientboundRaceAborted()},
            {NetworkMessageType.ClientboundRaceForcedToFinish, () => new ClientboundRaceForcedToFinish()},

            {NetworkMessageType.ServerboundAnimation, () => new ServerboundAnimation()},
            {NetworkMessageType.ServerboundPing, () => new ServerboundPing()},
            {NetworkMessageType.ServerboundPlayerHello, () => new ServerboundPlayerHello()},
            {NetworkMessageType.ServerboundPositionUpdate, () => new ServerboundPositionUpdate()},
            {NetworkMessageType.ServerboundScoreUpdate, () => new ServerboundScoreUpdate()},
            {NetworkMessageType.ServerboundVisualUpdate, () => new ServerboundVisualUpdate()},
            {NetworkMessageType.ServerboundEncounterRequest, () => new ServerboundEncounterRequest()},
            {NetworkMessageType.ServerboundReadyForRace, () => new ServerboundReadyForRace()},
            {NetworkMessageType.ServerboundFinishedRace, () => new ServerboundFinishedRace()}
        };

    public static NetworkPacket Read(byte[] data) {
        using var ms = new MemoryStream(data);
        using var br = new BinaryReader(ms);

        var packetType = (NetworkMessageType) br.ReadInt32();
        if (!PacketConstructors.TryGetValue(packetType, out var constructor)) {
            throw new Exceptions.UnknownPacketException(packetType);
        }

        var packet = constructor();

        try {
            packet.Read(br);
        } catch (Exception e) {
            throw new Exceptions.PacketSerializeException(packetType, e);
        }

        return packet;
    }

    public byte[] Serialize() {
        using var ms = new MemoryStream();
        using (var bw = new BinaryWriter(ms, Encoding.Default, leaveOpen: true)) {
            bw.Write((int) this.MessageType);
            this.Write(bw);
        }

        return ms.Length == ms.Capacity ? ms.GetBuffer() : ms.ToArray();
    }

    public static string DebugBytes(string place, byte[] bytes) {
        var hex = BitConverter.ToString(bytes).Replace("-", " ");
        return $"[{place}] {hex}";
    }
}
