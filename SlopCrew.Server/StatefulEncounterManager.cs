using System.Collections.Concurrent;
using Serilog;
using SlopCrew.Common;
using SlopCrew.Common.Network;
using SlopCrew.Common.Network.Serverbound;
using SlopCrew.Common.Race;
using SlopCrew.Server.Race;
using Swan;

namespace SlopCrew.Server; 

public class StatefulEncounterManager {
    
    public CancellationToken CancellationToken { get; private set; }
    public CancellationTokenSource? CancellationTokenSource { get; private set; }
    private readonly ConcurrentDictionary<Guid, StatefulEncounter> encounters = new();
    
    public void Update() {
        // Regular update
        this.encounters.ForEach((encounterId, encounter) => {
            var req = encounter.Update();
            req.ForEach(packet => encounter.SendToAllPlayers(packet));
        });

        // Check to remove empty races
        this.encounters.Where(kv => kv.Value.IsEmpty())
            .ToList()
            .ForEach(kv => {
                kv.Value.State = EncounterState.Finished;
            });

        int removed = 0;

        this.encounters.Where(kv => kv.Value.State == EncounterState.Finished)
            .Select(e => e.Key)
            .ToList()
            .ForEach(key => {
                if (this.encounters.TryRemove(key, out var removedRace)) {
                    removed++;
                }
            });

        if (removed > 0) {
            Log.Information($"{DateTime.UtcNow} : {removed} encounters were removed");
        }
    }

    public StatefulEncounter? PlayerCurrentEncounterOfType(ConnectionState player, EncounterType encounterType) {
        foreach (var kv in this.encounters.Where(kv => kv.Value.EncounterType == encounterType)) {
            var encounter = kv.Value;
            if (encounter.Players.Contains(player)) {
                return kv.Value;
            }
        }
        return null;
    }

    public void EncounterSpecificPacket(ConnectionState state, NetworkPacket msg) {
        if (state.Player == null) {
            return;
        }
        
        switch (msg) {
            case ServerboundFinishedRace serverboundFinishedRace:
                var encounter = this.PlayerCurrentEncounterOfType(state, EncounterType.RaceEncounter);
                if (encounter != null) {
                    var race = encounter as RaceStatefulEncounter;
                    if (race == null) {
                        Log.Warning($"Invalid EncounterType found while attempting to cast as RaceStatefulEncounter");
                        return;
                    }
                    race.AddPlayerTime(state.Player.ID, serverboundFinishedRace.Time);
                }
                break;
        }
    }
}
