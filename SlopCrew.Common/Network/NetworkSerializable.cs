using System.IO;

namespace SlopCrew.Common.Network;

public abstract class NetworkSerializable {
    public abstract void Read(BinaryReader br);
    public abstract void Write(BinaryWriter bw);
}
