using Serilog;
using SlopCrew.Common;
using SlopCrew.Common.Network;
using SlopCrew.Common.Network.Clientbound;
using SlopCrew.Common.Race;

namespace SlopCrew.Server.Race {
    public class RaceStatefulEncounter : StatefulEncounter {
        public string Name { get; set; } = "";

        public RaceState State { get; set; } = RaceState.None;

        public RaceConfig Config { get; set; } = new RaceConfig();

        public ICollection<Player> Players { get; set; } = new List<Player>();

        public DateTime Initialized { get; set; }

        public DateTime Started;

        public DateTime Racing;

        public int ConfirmedPlayers { get; set; } = 0;

        public Dictionary<uint, float> Ranking { get; set; } = new Dictionary<uint, float>();

        private const int MAX_PLAYERS_IN_RACE = 32;                       //TODO: Configurable may be 
        private const int MAX_WAITING_JOINING_TIME_FOR_SECS = 20;         //TODO: Configurable may be 
        private const int MAX_WAITING_PLAYERS_TO_BE_READY_TIME_SECS = 20; //TODO: Configurable may be 
        private const int MAX_WAITING_PLAYERS_TO_FINISH_TIME_SECS = 603;  //TODO: Configurable may be

        public RaceStatefulEncounter(Guid raceId) : base(raceId) { }

        public override List<NetworkPacket> Update() {
            var req = new List<NetworkPacket>();
            switch (this.State) {
                case RaceState.WaitingForPlayers:
                    var remainingTime = DateTime.UtcNow - this.Initialized;

                    if (remainingTime > TimeSpan.FromSeconds(MAX_WAITING_JOINING_TIME_FOR_SECS)) {
                        if (this.Players.Count() < 2 && !Server.Instance.IsDebug()) {
                            Log.Information($"Race {this.EncounterId} has 1 player. Aborting race...");

                            req.Add(new ClientboundRaceAborted());
                            this.State = RaceState.Aborted;
                        } else {
                            this.State = RaceState.Starting;
                        }
                    }

                    break;
                case RaceState.Starting:
                    var clientBoundRaceInitialize = new ClientboundRaceInitialize();
                    req.Add(clientBoundRaceInitialize);

                    this.State = RaceState.WaitingForPlayersToBeReady;
                    this.Started = DateTime.UtcNow;

                    break;
                case RaceState.WaitingForPlayersToBeReady:
                    var remainingWaitingTime = DateTime.UtcNow - this.Started;

                    if (remainingWaitingTime > TimeSpan.FromSeconds(MAX_WAITING_PLAYERS_TO_BE_READY_TIME_SECS)
                        || this.ConfirmedPlayers == this.Players.Count) {

                        if (this.Players.Count() < 2 && !Server.Instance.IsDebug()) {
                            Log.Information($"Race {this.EncounterId} has 1 player. Aborting race...");

                            req.Add(new ClientboundRaceAborted());
                            this.State = RaceState.Aborted;
                        } else {
                            var clientBoundRaceStart = new ClientboundRaceStart();
                            req.Add(clientBoundRaceStart);

                            this.Racing = DateTime.UtcNow;
                            this.State = RaceState.Racing;

                            Log.Information($"race {this.EncounterId} started at {this.Racing}");
                        }
                    }

                    break;
                case RaceState.Racing:
                    var remainingWaitingBeforeFinishingTime = DateTime.UtcNow - this.Racing;

                    if (remainingWaitingBeforeFinishingTime >
                        TimeSpan.FromSeconds(MAX_WAITING_PLAYERS_TO_FINISH_TIME_SECS)
                        || this.Ranking.Count == this.Players.Count) {

                        var sortedRanking = this.Ranking.OrderBy(kv => kv.Value).ToList();

                        var connexions = Server.Instance.GetConnections();

                        var rank = sortedRanking.Select((kv) => {
                            var playerName = this.Players.Where(p => p.ID == kv.Key).FirstOrDefault()?.Name;
                            return (playerName, kv.Value);
                        }).ToList();

                        if (this.Ranking.Count == this.Players.Count) {
                            req.Add(new ClientboundRaceRank {
                                                             Rank = rank
                                                         });

                            this.State = RaceState.Finished;
                        } else {
                            req.Add(new ClientboundRaceForcedToFinish {
                                                                       Rank = rank
                                                                   });

                            this.State = RaceState.ForcedFinish;
                        }

                        Log.Information($"race {this.EncounterId} finished at {DateTime.UtcNow}");
                    }

                    break;
                case RaceState.Finished:
                    break;
            }

            return req;
        }
        
        

        public void AddPlayerTime(uint id, float time) {
            if (!this.Players.Select(p => p.ID).Contains(id)) {
                Log.Information($"Player {id} is not racing");
                return;
            }

            if (this.Ranking.ContainsKey(id)) {
                Log.Information($"Player {id} already finished");
                return;
            }

            this.Ranking.Add(id, time);
        }
    }
}
