using System.Collections.Generic;
using System.IO;
using SlopCrew.Common.Network;

namespace SlopCrew.Common.Encounters;

public class EncounterEndData : NetworkSerializable {
    public override void Read(BinaryReader br) { }
    public override void Write(BinaryWriter bw) { }

    public static EncounterEndData Read(EncounterType type, BinaryReader br) {
        var data = type switch {
            EncounterType.RaceEncounter => new RaceEncounterEndData(),
            _ => new EncounterEndData()
        };
        data.Read(br);
        return data;
    }
}

public class RaceEncounterEndData : EncounterEndData {
    public Dictionary<uint, float> Rankings;

    public override void Read(BinaryReader br) {
        var len = br.ReadInt32();
        this.Rankings = new(len);
        for (var i = 0; i < len; i++) {
            var key = br.ReadUInt32();
            var value = br.ReadSingle();
            this.Rankings.Add(key, value);
        }
    }

    public override void Write(BinaryWriter bw) {
        bw.Write(this.Rankings.Count);
        foreach (var kvp in this.Rankings) {
            bw.Write(kvp.Key);
            bw.Write(kvp.Value);
        }
    }
}
