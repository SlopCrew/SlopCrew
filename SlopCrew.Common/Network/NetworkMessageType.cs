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
    ClientboundRequestRace,
    ClientboundRaceInitialize,
    ClientboundRaceStart,
    ClientboundRaceRank,

    ServerboundAnimation,
    ServerboundPing,
    ServerboundPlayerHello,
    ServerboundPositionUpdate,
    ServerboundScoreUpdate,
    ServerboundVisualUpdate,
    ServerboundRequestRace,
    ServerboundReadyForRace,
    ServerboundFinishedRace
}
