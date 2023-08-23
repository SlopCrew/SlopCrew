namespace SlopCrew.Common.Network;

public enum NetworkMessageType {
    ClientboundPlayerAnimation,
    ClientboundPlayerPositionUpdate,
    ClientboundPlayersUpdate,
    ServerboundAnimation,
    ServerboundPlayerHello,
    ServerboundPositionUpdate
}
