using System;

namespace SlopCrew.Common;

public enum EncounterType {
    ScoreEncounter,
    ComboEncounter,
    RaceEncounter
}

public struct Encounter {
    public EncounterType EncounterType;
    public IStatefullApp? State;

    public static bool IsStatefullEncounter(EncounterType type) {
        return type switch {
            EncounterType.ScoreEncounter => false,
            EncounterType.ComboEncounter => false,
            EncounterType.RaceEncounter => true,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}
