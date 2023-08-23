namespace SlopCrew.Common.Network.Clientbound;

public class ClientboundPlayerAnimation : NetworkMessage {
    public ClientboundPlayerAnimation() { }

    public string Player;
    public int Animation;
    public bool ForceOverwrite;
    public bool Instant;
    public float AtTime;
}
