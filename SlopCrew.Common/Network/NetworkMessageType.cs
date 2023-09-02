namespace SlopCrew.Common.Network;

public enum NetworkMessageType {
    // Keep this at zero just in case the others shift
    ServerboundVersion,

    ClientboundPlayerAnimation,
    ClientboundPlayerPositionUpdate,
    ClientboundPlayerScoreUpdate,
    ClientboundPlayersUpdate,
    ClientboundPlayerVisualUpdate,
    ClientboundPong,
    ClientboundSync,
    ClientboundEncounterStart,

    ServerboundAnimation,
    ServerboundPing,
    ServerboundPlayerHello,
    ServerboundPositionUpdate,
    ServerboundScoreUpdate,
    ServerboundVisualUpdate,
    ServerboundEncounterRequest,
    
    // down here rn to have backwards-compat
    ClientboundEncounterRequest
}
