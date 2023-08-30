namespace SlopCrew.Common.Network;

public enum NetworkMessageType {
    // Keep this at zero just in case the others shift
    ServerboundVersion,

    ClientboundPlayerAnimation,
    ClientboundPlayerPositionUpdate,
    ClientboundPlayersUpdate,
    ClientboundPlayerVisualUpdate,
    ClientboundSync,
    ClientboundPong,

    ServerboundAnimation,
    ServerboundPlayerHello,
    ServerboundPositionUpdate,
    ServerboundVisualUpdate,
    ServerboundPing
}
