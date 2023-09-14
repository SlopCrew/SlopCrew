using Serilog;
using SlopCrew.Common;
using SlopCrew.Common.Network;
using SlopCrew.Common.Network.Clientbound;
using SlopCrew.Common.Encounters;

namespace SlopCrew.Server.Race;

public class RaceStatefulEncounter : StatefulEncounter {
    public string Name = string.Empty;
    public RaceConfig ConfigData;
    public Dictionary<uint, float> Ranking { get; set; } = new();

    public RaceStatefulEncounter(int stage) : base(stage) {
        var pool = Server.Instance.StatefulEncounterManager.RaceConfigs
            .Where(x => x.Stage == stage)
            .ToList();
        var random = new Random();
        var index = random.Next(0, pool.Count);
        this.ConfigData = pool[index];
    }

    public const int MaxRaceTime = 600;

    public override void Update() {
        base.Update();
        var remainingWaitingBeforeFinishingTime = DateTime.UtcNow - this.StartTime;

        if (
            remainingWaitingBeforeFinishingTime > TimeSpan.FromSeconds(MaxRaceTime)
            || this.Ranking.Count == this.Players.Count
        ) {
            this.SendToAllPlayers(new ClientboundEncounterEnd {
                EncounterType = EncounterType.RaceEncounter,
                EndData = new RaceEncounterEndData {
                    Rankings = this.Ranking
                }
            });
            this.State = EncounterState.Finished;
        }
    }

    public void AddPlayerTime(uint id, float time) {
        this.Ranking.Add(id, time);
    }
}
