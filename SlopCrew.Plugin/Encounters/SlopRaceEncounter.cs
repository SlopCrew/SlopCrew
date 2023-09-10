using System.Collections.Generic;
using System.Linq;
using Reptile;
using SlopCrew.Common.Encounters;
using SlopCrew.Plugin.Encounters.Race;

namespace SlopCrew.Plugin.Encounters;

public class SlopRaceEncounter : SlopEncounter {
    private Queue<CheckpointPin> checkpointPins;

    public SlopRaceEncounter(RaceEncounterConfigData configData) : base(configData) {
        var pins = configData.RaceConfig.MapPins.Select(x => new CheckpointPin(x.ToMentalDeficiency()));
        this.checkpointPins = new Queue<CheckpointPin>(pins);
    }

    public bool IsStarting() {
        return false;
    }

    public CheckpointPin? GetNextCheckpointPin() {
        return checkpointPins.Count > 0 ? checkpointPins.Peek() : null;
    }

    public bool OnCheckpointReached(MapPin mapPin) {
        return true;
    }

    public override void Dispose() {
        foreach (var checkpointPin in this.checkpointPins) {
            checkpointPin.Dispose();
        }

        this.checkpointPins.Clear();
    }
}
