using System.Numerics;

namespace SlopCrew.Common.Network.Serverbound;

public class ServerboundPositionUpdate : NetworkMessage {
    public ServerboundPositionUpdate() { }

    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Velocity;
}
