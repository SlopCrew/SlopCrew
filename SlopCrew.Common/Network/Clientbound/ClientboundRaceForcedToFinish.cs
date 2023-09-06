using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SlopCrew.Common.Network.Clientbound {
    public class ClientboundRaceForcedToFinish : NetworkPacket {
        public override NetworkMessageType MessageType => NetworkMessageType.ClientboundRaceForcedToFinish;

        public IEnumerable<(string? playerName, float time)> Rank { get; set; } = new List<(string? playerName, float time)>();

        public override void Read(BinaryReader br) {
            var count = br.ReadInt32();
            var rank = new List<(string? playerName, float time)>();
            for (var i = 0; i < count; i++) {
                var playerName = br.ReadString();
                var time = br.ReadSingle();
                rank.Add((playerName, time));
            }

            Rank = rank;
        }

        public override void Write(BinaryWriter bw) {
            bw.Write(Rank.Count());
            foreach (var (playerName, time) in Rank) {
                bw.Write(playerName);
                bw.Write(time);
            }
        }
    }
}
