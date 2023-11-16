using Microsoft.Extensions.Options;
using SlopCrew.Common.Proto;
using SlopCrew.Server.Options;

namespace SlopCrew.Server.Encounters;

public class EncounterService : IDisposable {
    public RaceConfigService RaceConfigService;
    
    private ILogger<EncounterService> logger;
    private EncounterOptions options;
    private TickRateService tickRateService;
    private List<Encounter> encounters = new();
    private Dictionary<(int, EncounterType), Lobby> lobbies = new();
    
    public EncounterService(ILogger<EncounterService> logger, TickRateService tickRateService, IOptions<EncounterOptions> options, RaceConfigService raceConfigService) {
        this.RaceConfigService = raceConfigService;
        this.logger = logger;
        this.tickRateService = tickRateService;
        this.options = options.Value;
        this.tickRateService.Tick += this.Tick;
    }

    public void Dispose() {
        this.tickRateService.Tick -= this.Tick;
    }

    private void Tick() {
        this.lobbies.Values.ToList().ForEach(x => x.Update());
        this.encounters.ForEach(x => x.Update());
        
        var finished = this.encounters.Where(x => x.Finished).ToList();
        finished.ForEach(x => x.Dispose());
        this.encounters = this.encounters.Where(x => !x.Finished).ToList();
    }

    public void QueueIntoLobby(NetworkClient client, EncounterType type) {
        if (client.Stage is null) return;
        if (type is EncounterType.Race && !this.RaceConfigService.HasRaceConfigForStage(client.Stage.Value)) return;
        
        (int, EncounterType) key = (client.Stage.Value, type);
        if (!this.lobbies.ContainsKey(key)) {
            this.lobbies[key] = new Lobby(this, client.Stage.Value, type);
        }
        
        this.lobbies[key].AddPlayer(client);
    }
    
    public Encounter StartSimpleEncounter(NetworkClient one, NetworkClient two, EncounterType type) {
        Encounter encounter = type switch {
            EncounterType.ScoreBattle => new ScoreBattleEncounter(one, two, this.options.ScoreBattleLength),
            EncounterType.ComboBattle => new ComboBattleEncounter(one, two, this.options.ComboBattleLength, this.options.ComboBattleGrace),
            _ => throw new Exception("TODO")
        };
        this.encounters.Add(encounter);
        return encounter;
    }

    public void TrackEncounter(Encounter encounter) {
        this.encounters.Add(encounter);
    }
}
