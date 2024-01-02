using UnityEngine;

namespace SlopCrew.Plugin;

public static class UnityExtensions {
    public static System.Numerics.Vector3 UnityToSystem(this Vector3 vec) {
        return new System.Numerics.Vector3(vec.x, vec.y, vec.z);
    }

    public static System.Numerics.Quaternion UnityToSystem(this Quaternion quat) {
        return new System.Numerics.Quaternion(quat.x, quat.y, quat.z, quat.w);
    }

    public static Common.Proto.Vector3 UnityToNetwork(this Vector3 vec) {
        return new Common.Proto.Vector3 {
            X = vec.x,
            Y = vec.y,
            Z = vec.z
        };
    }

    public static Common.Proto.Quaternion UnityToNetwork(this Quaternion quat) {
        return new Common.Proto.Quaternion {
            X = quat.x,
            Y = quat.y,
            Z = quat.z,
            W = quat.w
        };
    }

    public static Vector3 SystemToUnity(this System.Numerics.Vector3 vec) {
        return new Vector3(vec.X, vec.Y, vec.Z);
    }

    public static Quaternion SystemToUnity(this System.Numerics.Quaternion quat) {
        return new Quaternion(quat.X, quat.Y, quat.Z, quat.W);
    }

    public static Common.Proto.Vector3 SystemToNetwork(this System.Numerics.Vector3 vec) {
        return new Common.Proto.Vector3 {
            X = vec.X,
            Y = vec.Y,
            Z = vec.Z
        };
    }

    public static Common.Proto.Quaternion SystemToNetwork(this System.Numerics.Quaternion quat) {
        return new Common.Proto.Quaternion {
            X = quat.X,
            Y = quat.Y,
            Z = quat.Z,
            W = quat.W
        };
    }

    public static Vector3 NetworkToUnity(this Common.Proto.Vector3 vec) {
        return new Vector3(vec.X, vec.Y, vec.Z);
    }

    public static Quaternion NetworkToUnity(this Common.Proto.Quaternion quat) {
        return new Quaternion(quat.X, quat.Y, quat.Z, quat.W);
    }

    public static System.Numerics.Vector3 NetworkToSystem(this Common.Proto.Vector3 vec) {
        return new System.Numerics.Vector3(vec.X, vec.Y, vec.Z);
    }

    public static System.Numerics.Quaternion NetworkToSystem(this Common.Proto.Quaternion quat) {
        return new System.Numerics.Quaternion(quat.X, quat.Y, quat.Z, quat.W);
    }

    public static string GetPath(this GameObject gameObject) {
        var path = $"/{gameObject.name}";
        while (gameObject.transform.parent != null) {
            gameObject = gameObject.transform.parent.gameObject;
            path = $"/{gameObject.name}{path}";
        }
        return path;
    }
}
