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
    ClientboundEncounterRequest,
    ClientboundEncounterStart,
    ClientboundRequestRace,
    ClientboundRaceAborted,
    ClientboundRaceInitialize,
    ClientboundRaceStart,
    ClientboundRaceRank,
    ClientboundRaceForcedToFinish,
    // will potentially be merged with the encounter packets?

    ServerboundAnimation,
    ServerboundPing,
    ServerboundPlayerHello,
    ServerboundPositionUpdate,
    ServerboundScoreUpdate,
    ServerboundVisualUpdate,
    ServerboundEncounterRequest,
    ServerboundReadyForRace,
    ServerboundFinishedRace
    // will potentially be merged with the encounter packets?
}
