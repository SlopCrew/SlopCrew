using System.Numerics;

namespace SlopCrew.Common.Network.Clientbound;

public class ClientboundPlayerPositionUpdate : NetworkMessage {
    public ClientboundPlayerPositionUpdate() { }

    public string Player;
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Velocity;
}
