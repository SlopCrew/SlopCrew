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

        private readonly ConcurrentDictionary<Guid, RaceStatefulEncounter> races = new();
        private static bool IsDebug = false;

        public static RacerManager Instance { get => GetInstance(); }

        private static RacerManager GetInstance() {
            if (instance == null) {
                instance = new RacerManager();
                IsDebug = Server.Instance.Config.Debug;
            }
            return instance;
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

            var newRace = new RaceStatefulEncounter {
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

        private RaceStatefulEncounter GetPlayerRace(uint? id) {
            return races.First(kv => kv.Value.Players.Any(
                player => player.ID == id)).Value;
        }
    }
}
