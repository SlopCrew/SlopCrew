using Serilog;
using SlopCrew.Common;
using SlopCrew.Common.Network.Clientbound;
using SlopCrew.Common.Race;
using Swan;
using System.Collections.Concurrent;
using System.Numerics;

namespace SlopCrew.Server.Race {
    public class Racer {
        private static Racer? instance;

        public CancellationToken CancellationToken { get; private set; }
        public CancellationTokenSource? CancellationTokenSource { get; private set; }

        private const int MAX_PLAYERS_IN_RACE = 32; //TODO: Configurable may be 
        private const int MAX_WAITING_JOINING_TIME_FOR_SECS = 5; //TODO: Configurable may be 
        private const int MAX_WAITING_PLAYERS_TO_BE_READY_TIME_SECS = 30; //TODO: Configurable may be 
        private const int MAX_WAITING_PLAYERS_TO_FINISH_TIME_SECS = 183; //TODO: Configurable may be

        private readonly ConcurrentDictionary<Guid, Race> races = new();

        public static Racer Instance { get => GetInstance(); }

        private static Racer GetInstance() {
            if (instance == null) {
                instance = new Racer();
            }
            return instance;
        }

        internal Requests Update() {
            var res = new Requests();

            //Regular update
            races.ForEach((raceId, race) => {
                switch (race.State) {
                    case RaceState.WaitingForPlayers:
                        var remainingTime = DateTime.UtcNow - race.Initialized;

                        Log.Information($"Remaining {MAX_WAITING_JOINING_TIME_FOR_SECS - remainingTime.Seconds} before trying to start race {raceId}");

                        if (remainingTime > TimeSpan.FromSeconds(MAX_WAITING_JOINING_TIME_FOR_SECS)) {
                            //if (v.Player.Count() < 2) {
                            //    Log.Information($"Race {k} has 1 player. Aborting race...");
                            //    v.State = RaceState.None;
                            //    return;
                            //}
                            race.State = RaceState.Starting;
                        }

                        break;
                    case RaceState.Starting:
                        var clientBoundRaceInitialize = new ClientboundRaceInitialize();
                        res.RaceInitializeRequests.Add((race.GetPlayersId(), clientBoundRaceInitialize));

                        race.State = RaceState.WaitingForPlayersToBeReady;
                        race.Started = DateTime.UtcNow;

                        break;
                    case RaceState.WaitingForPlayersToBeReady:
                        var remainingWaitingTime = DateTime.UtcNow - race.Started;

                        Log.Information($" {race.ConfirmedPlayers}/{race.Players.Count} Remaining {MAX_WAITING_PLAYERS_TO_BE_READY_TIME_SECS - remainingWaitingTime.Seconds} before trying to actually start race {raceId}");

                        if (remainingWaitingTime > TimeSpan.FromSeconds(MAX_WAITING_PLAYERS_TO_BE_READY_TIME_SECS)
                            || race.ConfirmedPlayers == race.Players.Count) {

                            //if (v.Player.Count() < 2) {
                            //    Log.Information($"Race {k} has 1 player. Aborting race...");
                            //    v.State = RaceState.None;
                            //    return;
                            //}

                            var clientBoundRaceStart = new ClientboundRaceStart();
                            res.RaceStartRequests.Add((race.GetPlayersId(), clientBoundRaceStart));

                            race.Racing = DateTime.UtcNow;
                            race.State = RaceState.Racing;

                            Log.Information($"race {raceId} started at {race.Racing}");
                        }

                        break;
                    case RaceState.Racing:
                        var remainingWaitingBeforeFinishingTime = DateTime.UtcNow - race.Racing;


                        if (remainingWaitingBeforeFinishingTime > TimeSpan.FromSeconds(MAX_WAITING_PLAYERS_TO_FINISH_TIME_SECS)
                            || race.Ranking.Count == race.Players.Count) {

                            var sortedRanking = race.Ranking.OrderBy(kv => kv.Value).ToList();

                            var connexions = Server.Instance.GetConnections();

                            var rank = sortedRanking.Select((kv) => {
                                var playerName = race.Players.Where(p => p.ID == kv.Key).FirstOrDefault()?.Name;
                                return (playerName, kv.Value);
                            }).ToList();

                            Log.Information($"Ranking: {rank.Count}");

                            res.RaceRankRequests.Add((race.GetPlayersId(), new ClientboundRaceRank {
                                Rank = rank
                            }));

                            Log.Information($"race {raceId} finished at {DateTime.UtcNow}");

                            race.State = RaceState.Finished;
                        }

                        break;
                    case RaceState.Finished:
                        break;
                }
            });

            //Check to remove empty races
            races.Where(kv => kv.Value.Players.Count == 0)
                .ToList()
                .ForEach(kv => {
                    kv.Value.State = RaceState.Finished;
                });

            int removed = 0;

            races.Where(entry => entry.Value.State == RaceState.Finished)
                 .Select(entry => entry.Key)
                 .ToList()
                 .ForEach(key => {
                     if (races.TryRemove(key, out var removedRace)) {
                         removed++;
                     }
                 });

            if (removed > 0) {
                Log.Information($"{removed} race where remove");
            }

            return res;
        }

        public void MarkPlayerReady(uint id) {
            if (!IsPlayerAlreadyRacing(id)) {
                Log.Information($"Player {id} is not racing");
                return;
            }

            //TODO: may be prevent double ready if it somehow happens
            var race = GetPlayerRace(id);

            if (race.State != RaceState.WaitingForPlayersToBeReady) {
                Log.Information($"Race is not waiting for players");
                return;
            }

            race.ConfirmedPlayers += 1;
        }

        public void AddPlayerTime(uint id, float time) {
            if (!IsPlayerAlreadyRacing(id)) {
                Log.Information($"Player {id} is not racing");
                return;
            }

            var race = GetPlayerRace(id);

            if (race.Ranking.ContainsKey(id)) {
                Log.Information($"Player {id} already finished");
                return;
            }

            race.Ranking.Add(id, time);
        }

        public bool IsPlayerAlreadyRacing(uint id) {
            return races.Any(kv => kv.Value.Players.Any(p => p.ID == id));
        }

        /// <summary>
        /// Useful to remove player on disconnect
        /// </summary>
        /// <param name="player"></param>
        internal void RemovePlayerIfRacing(Player player) {
            if (player == null) {
                return;
            }

            if (!IsPlayerAlreadyRacing(player.ID)) {
                Log.Information($"Player {player.ID} is not racing");
                return;
            }

            var race = GetPlayerRace(player.ID);
            race.Players.Remove(player);
        }

        public RaceConfig? GetARace(Player player) {
            if (IsPlayerAlreadyRacing(player.ID)) {
                return null;
            }

            var availableRace = races.FirstOrDefault(kv =>
                kv.Value.State == RaceState.WaitingForPlayers
                && kv.Value.Players.Count() < MAX_PLAYERS_IN_RACE);

            var raceConf = GetANewRaceConf();

            if (availableRace.Key != Guid.Empty) {
                availableRace.Value.Players.Add(player);
                return raceConf;
            }

            var res = races.TryAdd(Guid.NewGuid(), new Race {
                Config = raceConf,
                Players = new List<Player> { player },
                State = RaceState.WaitingForPlayers,
                Initialized = DateTime.UtcNow
            });

            if (res) {
                return raceConf;
            }

            Log.Error("Failed to get a new race");

            return null;
        }

        private RaceConfig GetANewRaceConf() {
            return new RaceConfig {
                Stage = 9,
                StartPosition = new Vector3(-0.94f, 13.70f, 3.71f),
                MapPins = new List<Vector3>
               {
                    new Vector3(-3.12f, 13.63f, -65.70f),
                    new Vector3(-1.59f, 13.58f, 60.85f),
                }
            };
        }

        private Race GetPlayerRace(uint id) {
            return races.First(kv => kv.Value.Players.Any(
                player => player.ID == id)).Value;
        }
    }
}
