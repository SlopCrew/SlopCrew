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
    
    // will potentially be merged with the encounter packets?
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
    ServerboundEncounterRequest,
    
    // will potentially be merged with the encounter packets?
    ServerboundRequestRace,
    ServerboundReadyForRace,
    ServerboundFinishedRace
}
