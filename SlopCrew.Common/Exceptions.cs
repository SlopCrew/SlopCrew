using System;
using SlopCrew.Common.Network;

namespace SlopCrew.Common;

public class Exceptions {
    public class UnknownPacketException : Exception {
        public UnknownPacketException(NetworkMessageType packetType) : base($"Unknown packet type {packetType}") { }
    }

    public class PacketSerializeException : Exception {
        public PacketSerializeException(NetworkMessageType packetType, Exception innerException) : base(
            $"Failed to (de)serialize packet {packetType}", innerException) { }
    }
}
