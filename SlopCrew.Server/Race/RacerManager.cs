using Serilog;
using SlopCrew.Common;
using SlopCrew.Common.Network.Clientbound;
using SlopCrew.Common.Race;
using SlopCrew.Server.Extensions;
using Swan;
using System.Collections.Concurrent;
using System.Text.Json;

namespace SlopCrew.Server.Race {
    public class RacerManager {
        private static RacerManager? instance;

        public CancellationToken CancellationToken { get; private set; }
        public CancellationTokenSource? CancellationTokenSource { get; private set; }

        private const int MAX_PLAYERS_IN_RACE = 32; //TODO: Configurable may be 
        private const int MAX_WAITING_JOINING_TIME_FOR_SECS = 20; //TODO: Configurable may be 
        private const int MAX_WAITING_PLAYERS_TO_BE_READY_TIME_SECS = 20; //TODO: Configurable may be 
        private const int MAX_WAITING_PLAYERS_TO_FINISH_TIME_SECS = 603; //TODO: Configurable may be

        private readonly ConcurrentDictionary<Guid, Race> races = new();
        private static bool IsDebug = false;

        public static RacerManager Instance { get => GetInstance(); }

        private static RacerManager GetInstance() {
            if (instance == null) {
                instance = new RacerManager();
                IsDebug = Server.Instance.Config.Debug;
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

                        if (remainingTime > TimeSpan.FromSeconds(MAX_WAITING_JOINING_TIME_FOR_SECS)) {
                            if (race.Players.Count() < 2 && !IsDebug) {
                                Log.Information($"Race {raceId} has 1 player. Aborting race...");

                                res.RaceAbortedRequests.Add((race!.GetPlayersId(), new ClientboundRaceAborted()));
                                race.State = RaceState.Aborted;
                            } else {
                                race.State = RaceState.Starting;
                            }
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

                        if (remainingWaitingTime > TimeSpan.FromSeconds(MAX_WAITING_PLAYERS_TO_BE_READY_TIME_SECS)
                            || race.ConfirmedPlayers == race.Players.Count) {

                            if (race.Players.Count() < 2 && !IsDebug) {
                                Log.Information($"Race {raceId} has 1 player. Aborting race...");

                                res.RaceAbortedRequests.Add((race!.GetPlayersId(), new ClientboundRaceAborted()));
                                race.State = RaceState.Aborted;
                            } else {
                                var clientBoundRaceStart = new ClientboundRaceStart();
                                res.RaceStartRequests.Add((race.GetPlayersId(), clientBoundRaceStart));

                                race.Racing = DateTime.UtcNow;
                                race.State = RaceState.Racing;

                                Log.Information($"race {raceId} started at {race.Racing}");
                            }
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

                            if (race.Ranking.Count == race.Players.Count) {
                                res.RaceRankRequests.Add((race.GetPlayersId(), new ClientboundRaceRank {
                                    Rank = rank
                                }));

                                race.State = RaceState.Finished;
                            } else {
                                res.RaceForcedToFinishRequests.Add((race.GetPlayersId(), new ClientboundRaceForcedToFinish {
                                    Rank = rank
                                }));

                                race.State = RaceState.ForcedFinish;
                            }

                            Log.Information($"race {raceId} finished at {DateTime.UtcNow}");
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

            races.Where(entry => entry.Value.State >= RaceState.Finished)
                 .Select(entry => entry.Key)
                 .ToList()
                 .ForEach(key => {
                     if (races.TryRemove(key, out var removedRace)) {
                         removed++;
                     }
                 });

            if (removed > 0) {
                Log.Information($"{DateTime.UtcNow} : {removed} race where removed");
            }

            return res;
        }

        public void MarkPlayerReady(uint id) {
            if (!IsPlayerRacing(id)) {
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
            if (!IsPlayerRacing(id)) {
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

        public bool IsPlayerRacing(uint? id) {
            return races.Any(kv => kv.Value.Players.Any(p => p.ID == id));
        }

        /// <summary>
        /// Useful to remove player on disconnect
        /// </summary>
        /// <param name="player"></param>
        internal void RemovePlayerIfRacing(uint? playerID) {
            if (!IsPlayerRacing(playerID)) {
                Log.Information($"Player {playerID} is not racing");
                return;
            }

            var race = GetPlayerRace(playerID);

            var player = race.Players.FirstOrDefault(p => p.ID == playerID)!;

            if (player != null) {
                race.Players.Remove(player);
            }
        }

        public (DateTime, RaceConfig?) GetARace(Player player) {
            if (IsPlayerRacing(player.ID)) {
                return (DateTime.MinValue, null);
            }

            var availableRace = races.FirstOrDefault(kv =>
                kv.Value.State == RaceState.WaitingForPlayers
                && kv.Value.Players.Count() < MAX_PLAYERS_IN_RACE);

            if (availableRace.Key != Guid.Empty) {
                availableRace.Value.Players.Add(player);
                return (availableRace.Value.Initialized.AddSeconds(MAX_WAITING_JOINING_TIME_FOR_SECS), availableRace.Value.Config);
            }

            (var name, var raceConf) = GetANewRaceConf();

            if (raceConf == null) {
                return (DateTime.MinValue, null);
            }

            var newRace = new Race {
                Config = raceConf!,
                Players = new List<Player> { player },
                State = RaceState.WaitingForPlayers,
                Initialized = DateTime.UtcNow,
                Name = name
            };

            var success = races.TryAdd(Guid.NewGuid(), newRace);

            if (success) {
                return (newRace.Initialized.AddSeconds(MAX_WAITING_JOINING_TIME_FOR_SECS), raceConf);
            }

            Log.Error("Failed to get a new race");

            return (DateTime.MinValue, null);
        }

        private (string, RaceConfig?) GetANewRaceConf() {
            return Task.Run(async () => {
                try {
                    var gh = new GitHubDownloader();

                    var races = await gh.DownloadFilesFromDirectory("SlopCrew", "race-config");

                    var sortedRaces = races.ToList();
                    sortedRaces.Shuffle();
                    var raceConf = sortedRaces.FirstOrDefault();

                    if (string.IsNullOrEmpty(raceConf.Key) || string.IsNullOrEmpty(raceConf.Value)) {
                        return ("", null);
                    }

                    RaceConfig desRaceConf = null;

                    try {
                        desRaceConf = JsonSerializer.Deserialize<RaceConfig>(raceConf.Value);
                    } catch (Exception ex) {
                        Log.Error("Couldn't deserialize race : ", ex);
                    }

                    return (raceConf.Key, desRaceConf);
                } catch (Exception e) {
                    Log.Error("Couldn't get a new race conf : ", e);
                    return ("", null);
                }
            }).GetAwaiter().GetResult();
        }

        private Race GetPlayerRace(uint? id) {
            return races.First(kv => kv.Value.Players.Any(
                player => player.ID == id)).Value;
        }
    }
}
