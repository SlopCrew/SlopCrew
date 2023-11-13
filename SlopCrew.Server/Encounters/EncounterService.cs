using Microsoft.Extensions.Options;
using SlopCrew.Common.Proto;
using SlopCrew.Server.Options;

namespace SlopCrew.Server.Encounters;

public class EncounterService : IDisposable {
    private ILogger<EncounterService> logger;
    private EncounterOptions options;
    
    private TickRateService tickRateService;
    private List<Encounter> encounters = new();
    
    public EncounterService(ILogger<EncounterService> logger, TickRateService tickRateService, IOptions<EncounterOptions> options) {
        this.logger = logger;
        this.tickRateService = tickRateService;
        this.options = options.Value;
        this.tickRateService.Tick += this.Tick;
    }

    public void Dispose() {
        this.tickRateService.Tick -= this.Tick;
    }

    private void Tick() {
        this.encounters.ForEach(x => x.Update());
        
        var finished = this.encounters.Where(x => x.Finished).ToList();
        finished.ForEach(x => x.Dispose());
        this.encounters = this.encounters.Where(x => !x.Finished).ToList();
    }
    
    public Encounter StartSimpleEncounter(NetworkClient one, NetworkClient two, EncounterType type) {
        var encounter = type switch {
            EncounterType.ScoreBattle => new ScoreBattleEncounter(one, two, this.options.ScoreBattleLength),
            _ => throw new Exception("TODO")
        };
        this.encounters.Add(encounter);
        return encounter;
    }
}
