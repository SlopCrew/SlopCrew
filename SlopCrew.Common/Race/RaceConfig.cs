using SlopCrew.Common.Network;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace SlopCrew.Common.Race {
    public class RaceConfig : NetworkSerializable {
        public int Stage { get; set; } = -1;
        public IEnumerable<SerDesVector3> MapPins { get; set; } = new List<SerDesVector3>();

        public SerDesVector3 StartPosition { get; set; } = new SerDesVector3(0, 0, 0);

        public override void Write(BinaryWriter bw) {
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

        public override void Read(BinaryReader br) {
            var stage = br.ReadInt32();

            var len = br.ReadInt32();
            var mapPins = new List<SerDesVector3>(len);
            for (var i = 0; i < len; i++) {
                mapPins.Add(new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()).ToSerDesVector3());
            }

            var startPosition = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()).ToSerDesVector3();

            Stage = stage;
            MapPins = mapPins;
            StartPosition = startPosition;
        }
    }
}
