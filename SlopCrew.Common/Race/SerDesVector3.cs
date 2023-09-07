using System.Numerics;

namespace SlopCrew.Common.Race {
    public class SerDesVector3 {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public SerDesVector3(float x, float y, float z) {
            X = x;
            Y = y;
            Z = z;
        }
    }

    public static class SerDesVector3Extensions {
        public static SerDesVector3 ToSerDesVector3(this Vector3 vector3) {
            return new SerDesVector3(vector3.X, vector3.Y, vector3.Z);
        }

        public static Vector3 ToVector3(this SerDesVector3 serDesVector3) {
            return new Vector3(serDesVector3.X, serDesVector3.Y, serDesVector3.Z);
        }
    }
}
