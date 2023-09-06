using System.IO;
using SlopCrew.Common.Network;

namespace SlopCrew.Common; 

public class EncounterConfig : NetworkSerializable {
    public EncounterType Type;
    public int PlayDuration;
    
    // Graffiti encounters
    public int[] GraffitiSpots;
    
    public static EncounterConfig ReadWithType(BinaryReader br) {
        var type = (EncounterType) br.ReadInt32();

        var config = type switch {
            EncounterType.ScoreEncounter => new EncounterConfig(),
            EncounterType.ComboEncounter => new EncounterConfig(),
            EncounterType.GraffitiEncounter => new GraffitiEncounterConfig()
        };
        
        config.Read(br);
        
        return config;
    }

    public override void Read(BinaryReader br) {
        this.Type = (EncounterType) br.ReadInt32();
        this.PlayDuration = br.ReadInt32();
    }

    public override void Write(BinaryWriter bw) {
        bw.Write((int) this.Type);
        bw.Write(this.PlayDuration);
    }
}

class GraffitiEncounterConfig : EncounterConfig {
    public override void Read(BinaryReader br) {
        base.Read(br);
        this.GraffitiSpots = new int[br.ReadInt32()];
        for (var i = 0; i < this.GraffitiSpots.Length; i++) {
            this.GraffitiSpots[i] = br.ReadInt32();
        }
    }
    
    public override void Write(BinaryWriter bw) {
        base.Write(bw);
        bw.Write(this.GraffitiSpots.Length);
        foreach (var graffitiSpot in this.GraffitiSpots) {
            bw.Write(graffitiSpot);
        }
    }
}

public enum EncounterType {
    ScoreEncounter,
    ComboEncounter,
    RaceEncounter,
    GraffitiEncounter
}
