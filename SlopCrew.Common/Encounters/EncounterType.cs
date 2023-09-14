namespace SlopCrew.Common;

public enum EncounterType {
    ScoreEncounter,
    ComboEncounter,
    RaceEncounter
}

public static class EncounterTypeExtensions {
    public static bool IsStateful(this EncounterType type) {
        return type is EncounterType.RaceEncounter;
    }
}
