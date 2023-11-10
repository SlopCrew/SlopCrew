namespace SlopCrew.Common.Proto;

public partial class Vector3 {
    public Vector3(System.Numerics.Vector3 vec) {
        this.X = vec.X;
        this.Y = vec.Y;
        this.Z = vec.Z;
    }
    
    public static implicit operator System.Numerics.Vector3(Vector3 vec) {
        return new System.Numerics.Vector3(vec.X, vec.Y, vec.Z);
    }
}
