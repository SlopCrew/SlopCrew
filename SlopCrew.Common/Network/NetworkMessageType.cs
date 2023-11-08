namespace SlopCrew.Common.Network;

public enum NetworkMessageType
{
    // Keep this at zero just in case the others shift
    ServerboundVersion,

    ClientboundEncounterEnd,
    ClientboundEncounterRequest,
    ClientboundEncounterCancel,
    ClientboundEncounterStart,
    ClientboundPlayerAnimation,
    ClientboundPlayerPositionUpdate,
    ClientboundPlayerScoreUpdate,
    ClientboundPlayersUpdate,
    ClientboundPlayerVisualUpdate,
    ClientboundPong,
    ClientboundSync,

    ServerboundAnimation,
    ServerboundEncounterRequest,
    ServerboundEncounterCancel,
    ServerboundPing,
    ServerboundPlayerHello,
    ServerboundPositionUpdate,
    ServerboundRaceFinish,
    ServerboundScoreUpdate,
    ServerboundVisualUpdate
}
