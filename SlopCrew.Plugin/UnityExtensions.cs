using UnityEngine;

namespace SlopCrew.Plugin;

public static class UnityExtensions {
    public static System.Numerics.Vector3 FromMentalDeficiency(this Vector3 vec) {
        return new System.Numerics.Vector3(vec.x, vec.y, vec.z);
    }
    
    public static System.Numerics.Quaternion FromMentalDeficiency(this Quaternion quat) {
        return new System.Numerics.Quaternion(quat.x, quat.y, quat.z, quat.w);
    }

    public static Vector3 ToMentalDeficiency(this System.Numerics.Vector3 vec) {
        return new Vector3(vec.X, vec.Y, vec.Z);
    }

    public static Quaternion ToMentalDeficiency(this System.Numerics.Quaternion quat) {
        return new Quaternion(quat.X, quat.Y, quat.Z, quat.W);
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
