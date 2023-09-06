using System.IO;
using SlopCrew.Common.Network;

namespace SlopCrew.Common; 

public class EncounterConfig : NetworkSerializable {
    public EncounterType Type;
    public int PlayDuration;
    public int[] GraffitiSpots;
    
    public override void Read(BinaryReader br) {}

    public override void Write(BinaryWriter bw) {
        throw new System.NotImplementedException();
    }
}

public enum EncounterType {
    ScoreEncounter,
    ComboEncounter,
    RaceEncounter,
    GraffitiEncounter
}
