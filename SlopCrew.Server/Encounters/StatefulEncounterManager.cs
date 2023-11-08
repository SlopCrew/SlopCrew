using Serilog;
using SlopCrew.Common;
using SlopCrew.Common.Encounters;
using SlopCrew.Common.Network.Clientbound;
using SlopCrew.Server.Race;
using System.Collections.Concurrent;
using System.Text.Json;

namespace SlopCrew.Server;

public class StatefulEncounterManager {
    public List<StatefulEncounter> Encounters = new();
    public ConcurrentDictionary<int, Dictionary<EncounterType, List<ConnectionState>>> QueuedPlayers = new();
    private ConcurrentDictionary<int, int> stageTicks = new();

    public List<RaceConfig> RaceConfigs = new();

    private const int TicksPerQueue = Constants.TicksPerSecond * 20;

    public StatefulEncounterManager() {
        Task.Run(this.DownloadRaceConfigs).Wait();
    }

    private async Task DownloadRaceConfigs() {
        Log.Information("Downloading race configs from GitHub...");
        var gh = new GitHubDownloader();
        var races = await gh.DownloadFilesFromDirectory("SlopCrew", "race-config");

        foreach (var file in races.Values) {
            var raceConfig = JsonSerializer.Deserialize<RaceConfig>(file, new JsonSerializerOptions {
                IncludeFields = true
            });
            if (raceConfig != null) this.RaceConfigs.Add(raceConfig);
        }

        Log.Information("Loaded {Count} race configs", this.RaceConfigs.Count);
    }

    public void Update() {
        // Regular update
        foreach (var encounter in this.Encounters) {
            encounter.Update();
        }

        // Check to remove empty races
        this.Encounters.Where(e => e.IsEmpty())
            .ToList()
            .ForEach(e => { e.State = EncounterState.Finished; });

        var finishedEncounters = this.Encounters
            .Where(e => e.State == EncounterState.Finished)
            .ToList();
        foreach (var encounter in finishedEncounters) {
            this.Encounters.Remove(encounter);
        }

        var queueIsEmpty = this.QueuedPlayers.Count == 0;
        if (!queueIsEmpty) {
            UpdateStageTicks();
        }
    }

    public void HandleEncounterRequest(ConnectionState conn, EncounterType type) {
        if (conn.Player is null) return;
        var stage = conn.Player.Stage;

        if (!this.QueuedPlayers.ContainsKey(stage)) this.QueuedPlayers[stage] = new();
        if (!this.QueuedPlayers[stage].ContainsKey(type)) this.QueuedPlayers[stage][type] = new();
        if (this.QueuedPlayers[stage][type].Contains(conn)) return;

        this.QueuedPlayers[stage][type].Add(conn);
    }

    public bool HandleEncounterCancel(ConnectionState conn, EncounterType type) {
        if (conn.Player is null) return false;

        switch (type) {
            case EncounterType.RaceEncounter:
                var stage = conn.Player.Stage;

                if (!this.QueuedPlayers.ContainsKey(stage)) return false;
                if (!this.QueuedPlayers[stage].ContainsKey(type)) return false;
                if (!this.QueuedPlayers[stage][type].Contains(conn)) return false;

                this.QueuedPlayers[stage][type].Remove(conn);

                if (this.QueuedPlayers[stage][type].Count == 0) {
                    this.QueuedPlayers[stage].Remove(type);
                }

                if (this.QueuedPlayers[stage].Count == 0) {
                    this.QueuedPlayers.Remove(stage, out var _);
                    this.stageTicks.Remove(stage, out var _);
                }

                return true;
            default:
                Log.Warning("Unknown encounter type {Type} to cancel", type);
                return false;
        }
    }

    private void UpdateStageTicks() {
        foreach (var (stage, _) in this.QueuedPlayers) {
            if (!this.stageTicks.ContainsKey(stage)) this.stageTicks[stage] = 0;
            this.stageTicks[stage]++;

            if (this.stageTicks[stage] >= TicksPerQueue) {
                this.QueuePlayersInStage(stage);
                this.stageTicks.Remove(stage, out var _);
                this.QueuedPlayers.Remove(stage, out var _);
            }
        }
    }

    private void QueuePlayersInStage(int stage) {
        foreach (var (type, players) in this.QueuedPlayers[stage]) {
            // Don't queue players who wandered into another stage
            var playersInStage = players.Where(x => x.Player!.Stage == stage).ToList();

            try {
                var encounter = type switch {
                    EncounterType.RaceEncounter => new RaceStatefulEncounter(stage),
                    _ => throw new ArgumentOutOfRangeException()
                };

                encounter.Players.AddRange(playersInStage);
                this.Encounters.Add(encounter);

                Log.Debug("Starting encounter {EncounterType} for {Count} players", encounter.EncounterType, playersInStage.Count);

                Server.Instance.Module.SendToTheConcerned(
                    playersInStage.Select(x => x.Player!.ID).ToList(),
                    new ClientboundEncounterStart {
                        EncounterType = type,
                        EncounterConfigData = new RaceEncounterConfigData {
                            EncounterLength = RaceStatefulEncounter.MaxRaceTime,
                            Guid = encounter.EncounterId,
                            RaceConfig = encounter.ConfigData
                        }
                    }
                );
            } catch (Exception e) {
                Log.Error(e, "Error while creating encounter");
            }
        }
    }
}
