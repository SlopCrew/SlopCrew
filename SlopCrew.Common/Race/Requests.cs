using SlopCrew.Common.Network.Clientbound;
using System.Collections.Generic;

namespace SlopCrew.Common.Race {
    public class Requests {

        public ICollection<(IEnumerable<uint>, ClientboundRaceInitialize)> RaceInitializeRequests { get; set; } = new List<(IEnumerable<uint>, ClientboundRaceInitialize)>();
        public ICollection<(IEnumerable<uint>, ClientboundRaceStart)> RaceStartRequests { get; set; } = new List<(IEnumerable<uint>, ClientboundRaceStart)>();
        public ICollection<(IEnumerable<uint>, ClientboundRaceRank)> RaceRankRequests { get; set; } = new List<(IEnumerable<uint>, ClientboundRaceRank)>();
        public ICollection<(IEnumerable<uint>, ClientboundRaceAborted)> RaceAbortedRequests { get; set; } = new List<(IEnumerable<uint>, ClientboundRaceAborted)>();
        public ICollection<(IEnumerable<uint>, ClientboundRaceForcedToFinish)> RaceForcedToFinishRequests { get; set; } = new List<(IEnumerable<uint>, ClientboundRaceForcedToFinish)>();
    }
}
