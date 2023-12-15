namespace SlopCrew.Common.Proto;

public partial class Quaternion {
    public Quaternion(System.Numerics.Quaternion quat) {
        this.X = quat.X;
        this.Y = quat.Y;
        this.Z = quat.Z;
        this.W = quat.W;
    }

    public static implicit operator System.Numerics.Quaternion(Quaternion quat) {
        return new System.Numerics.Quaternion(quat.X, quat.Y, quat.Z, quat.W);
    }
}
