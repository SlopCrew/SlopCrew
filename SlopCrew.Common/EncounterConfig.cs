using System.IO;
using SlopCrew.Common.Network;

namespace SlopCrew.Common; 

public abstract class EncounterConfig : NetworkSerializable {
    public abstract EncounterType Type { get; }
    public int PlayDuration;
    
    public static EncounterConfig ReadWithType(BinaryReader br) {
        var type = (EncounterType) br.ReadInt32();

        EncounterConfig config = type switch {
            EncounterType.ScoreEncounter => new ScoreEncounterConfig(),
            EncounterType.ComboEncounter => new ComboEncounterConfig(),
            EncounterType.GraffitiEncounter => new GraffitiEncounterConfig()
        };
        
        config.Read(br);
        
        return config;
    }

    public override void Read(BinaryReader br) {
        this.PlayDuration = br.ReadInt32();
    }

    public override void Write(BinaryWriter bw) {
        bw.Write((int) this.Type);
        bw.Write(this.PlayDuration);
    }
}

public class ScoreEncounterConfig : EncounterConfig {
    public override EncounterType Type => EncounterType.ScoreEncounter;

    public ScoreEncounterConfig() {
        this.PlayDuration = 180;
    }
}

public class ComboEncounterConfig : EncounterConfig {
    public override EncounterType Type => EncounterType.ComboEncounter;
    
    public ComboEncounterConfig() {
        this.PlayDuration = 300;
    }
}

public class GraffitiEncounterConfig : EncounterConfig {
    public override EncounterType Type => EncounterType.GraffitiEncounter;
    public int[] GraffitiSpots = {0, 1, 2, 3, 4};
    
    public GraffitiEncounterConfig() {
        this.PlayDuration = 120;
    }
    
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
