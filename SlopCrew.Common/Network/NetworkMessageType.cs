namespace SlopCrew.Common.Network;

public enum NetworkMessageType {
    ClientboundPlayerAnimation,
    ClientboundPlayerPositionUpdate,
    ClientboundPlayersUpdate,
    ClientboundPlayerVisualUpdate,

    ServerboundAnimation,
    ServerboundPlayerHello,
    ServerboundPositionUpdate,
    ServerboundVisualUpdate
}
