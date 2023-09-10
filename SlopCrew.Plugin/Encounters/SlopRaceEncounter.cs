using System.Collections.Generic;
using SlopCrew.Plugin.Encounters.Race;

namespace SlopCrew.Plugin.Encounters;

public class SlopRaceEncounter : SlopEncounter {
    private Queue<CheckpointPin> checkpointPins = new();

    public bool IsStarting() {
        return false;
    }
    
    public CheckpointPin? GetNextCheckpointPin() {
        return checkpointPins.Count > 0 ? checkpointPins.Peek() : null;
    }

    public override void Dispose() {
        foreach (var checkpointPin in this.checkpointPins) {
            checkpointPin.Dispose();
        }
        
        this.checkpointPins.Clear();
    }
}
