namespace SlopCrew.Common.Network;

public enum NetworkMessageType {
    ClientboundPlayerAnimation,
    ClientboundPlayerPositionUpdate,
    ClientboundPlayersUpdate,
    ClientboundPlayerVisualUpdate,
    ClientboundSync,

    ServerboundAnimation,
    ServerboundPlayerHello,
    ServerboundPositionUpdate,
    ServerboundVisualUpdate
}
