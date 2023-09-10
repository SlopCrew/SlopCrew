using System.Collections.Generic;
using System.IO;
using System.Numerics;
using SlopCrew.Common.Network;

namespace SlopCrew.Common.Encounters;

public class RaceConfig : NetworkSerializable {
    public int Stage;
    public List<Vector3> MapPins;
    public Vector3 StartPosition;

    public override void Write(BinaryWriter bw) {
        bw.Write(this.Stage);

        bw.Write(this.MapPins.Count);
        foreach (var mapPin in this.MapPins) bw.Write(mapPin);

        bw.Write(this.StartPosition);
    }

    public override void Read(BinaryReader br) {
        this.Stage = br.ReadInt32();

        var len = br.ReadInt32();
        this.MapPins = new(len);
        for (var i = 0; i < len; i++) {
            this.MapPins.Add(br.ReadVector3());
        }

        this.StartPosition = br.ReadVector3();
    }
}
