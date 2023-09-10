using SlopCrew.Common;
using SlopCrew.Common.Race;

namespace SlopCrew.Server.Race {
    internal class Race {
        public string Name { get; set; } = "";

        public RaceState State { get; set; } = RaceState.None;

        public RaceConfig Config { get; set; } = new RaceConfig();

        public ICollection<Player> Players { get; set; } = new List<Player>();

        public DateTime Initialized { get; set; }

        public DateTime Started;

        public DateTime Racing;

        public int ConfirmedPlayers { get; set; } = 0;

        public Dictionary<uint, float> Ranking { get; set; } = new Dictionary<uint, float>();

        public IEnumerable<uint> GetPlayersId() {
            return Players.Select(p => p.ID).ToList();
        }
    }
}
