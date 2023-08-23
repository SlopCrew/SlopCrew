namespace SlopCrew.Common.Network.Serverbound;

public class ServerboundAnimation : NetworkMessage {
    public ServerboundAnimation() { }

    public int Animation;
    public bool ForceOverwrite;
    public bool Instant;
    public float AtTime;
}
