using System.IO;
using System.Numerics;

namespace SlopCrew.Common.Network;

public static class NetworkExtensions {
    public delegate void LogDelegate(string msg);
    public static LogDelegate? Log = null;

    public static Vector3 ReadVector3(this BinaryReader br) {
        var x = br.ReadSingle();
        var y = br.ReadSingle();
        var z = br.ReadSingle();
        Log?.Invoke($"ReadVector3: {x}, {y}, {z}");
        return new Vector3(x, y, z);
    }

    public static Quaternion ReadQuaternion(this BinaryReader br) {
        var x = br.ReadSingle();
        var y = br.ReadSingle();
        var z = br.ReadSingle();
        var w = br.ReadSingle();
        return new Quaternion(x, y, z, w);
    }

    public static void Write(this BinaryWriter bw, Vector3 v) {
        Log?.Invoke($"Write(Vector3): {v.X}, {v.Y}, {v.Z}");
        bw.Write(v.X);
        bw.Write(v.Y);
        bw.Write(v.Z);
    }

    public static void Write(this BinaryWriter bw, Quaternion q) {
        bw.Write(q.X);
        bw.Write(q.Y);
        bw.Write(q.Z);
        bw.Write(q.W);
    }
}
