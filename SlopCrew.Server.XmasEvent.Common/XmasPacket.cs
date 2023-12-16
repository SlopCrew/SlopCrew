using System.Text;

namespace SlopCrew.Server.XmasEvent;

public abstract class XmasPacket {
    public abstract string GetPacketId();
    protected abstract uint LatestVersion { get; }
    public uint? PlayerID;
    public uint Version;

    public XmasPacket() {
        this.Version = this.LatestVersion;
    }

    public byte[] Serialize() {
        var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, false)) {
            writer.Write((UInt16) this.Version);
            this.Write(writer);
        }
        return stream.ToArray();
    }

    /// <summary>
    /// Write packet to bytes. Packet version has already been written.
    /// </summary>
    protected virtual void Write(BinaryWriter writer) {
        throw new NotImplementedException();
    }

    public void Deserialize(byte[] data) {
        var stream = new MemoryStream(data, false);
        using (var reader = new BinaryReader(stream, Encoding.UTF8)) {
            this.Version = reader.ReadUInt16();
            this.Read(reader);
        }
    }

    /// <summary>
    /// Parse packet from bytes. Packet version has already been read and set.
    /// </summary>
    protected virtual void Read(BinaryReader reader) {
        throw new NotImplementedException();
    }

    protected void UnexpectedVersion() {
        throw new XmasPacketParseException($"Got packet with unexpected version: {this.GetType().Name} {this.Version}");
    }
    
}
public class XmasPacketParseException : Exception {
    public XmasPacketParseException(string message) : base(message) {}
}
