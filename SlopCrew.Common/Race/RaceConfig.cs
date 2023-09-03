using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace SlopCrew.Common.Race {
    [Serializable]
    public class RaceConfig {
        public int Stage { get; set; } = -1;
        public IEnumerable<Vector3> MapPins { get; set; } = new List<Vector3>();

        public Vector3 StartPosition { get; set; }

        public void Write(BinaryWriter bw) {
            bw.Write(Stage);
            bw.Write(MapPins.Count());
            foreach (var mapPin in MapPins) {
                bw.Write(mapPin.X);
                bw.Write(mapPin.Y);
                bw.Write(mapPin.Z);
            }
            bw.Write(StartPosition.X);
            bw.Write(StartPosition.Y);
            bw.Write(StartPosition.Z);
        }

        public void Read(BinaryReader br) {
            var stage = br.ReadInt32();

            var len = br.ReadInt32();
            var mapPins = new List<Vector3>(len);
            for (var i = 0; i < len; i++) {
                mapPins.Add(new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()));
            }

            var startPosition = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());

            Stage = stage;
            MapPins = mapPins;
            StartPosition = startPosition;
        }
    }
}
