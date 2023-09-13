using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using SlopCrew.Common.Network;

namespace SlopCrew.Common.Encounters;

public class EncounterConfigData : NetworkSerializable {
    public float EncounterLength;
    public Guid Guid;

    public override void Read(BinaryReader br) {
        this.EncounterLength = br.ReadSingle();
        this.Guid = new Guid(br.ReadBytes(16));
    }

    public override void Write(BinaryWriter bw) {
        bw.Write(this.EncounterLength);
        bw.Write(this.Guid.ToByteArray());
    }

    public static EncounterConfigData Read(EncounterType type, BinaryReader br) {
        var data = type switch {
            EncounterType.ComboEncounter or EncounterType.ScoreEncounter => new SimpleEncounterConfigData(),
            EncounterType.RaceEncounter => new RaceEncounterConfigData(),
            _ => new EncounterConfigData()
        };
        data.Read(br);
        return data;
    }
}

public class SimpleEncounterConfigData : EncounterConfigData { }

public class RaceEncounterConfigData : EncounterConfigData {
    public RaceConfig RaceConfig;

    public override void Read(BinaryReader br) {
        base.Read(br);
        this.RaceConfig = new RaceConfig();
        this.RaceConfig.Read(br);
    }

    public override void Write(BinaryWriter bw) {
        base.Write(bw);
        this.RaceConfig.Write(bw);
    }
}
