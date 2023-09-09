using System.Text;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using SlopCrew.Common;
using SlopCrew.Common.Network.Clientbound;

namespace SlopCrew.Server;

// dynamic is a sin but I'm using it anyways. FIXME
public class SlopAPIController : WebApiController {
    [Route(HttpVerbs.Get, "/metrics")]
    public async Task GetMetrics() {
        var metrics = Server.Instance.Metrics;
        var response = new {
            connections = metrics.Connections,
            population = metrics.Population
        };

        await this.HttpContext.SendDataAsync(response);
    }

    [Route(HttpVerbs.Get, "/admin/players")]
    public async Task GetAdminPlayers() {
        var connections = Server.Instance.Module.Connections.Values;
        var result = new List<dynamic>();
        foreach (var connection in connections) {
            var player = connection.Player;
            var scoreUpdate = connection.LastScoreUpdate;
            result.Add(new {
                name = player?.Name,
                id = player?.ID,
                stage = player?.Stage,
                score = scoreUpdate is null
                            ? null
                            : new {
                                points = scoreUpdate.Score,
                                baseScore = scoreUpdate.BaseScore,
                                multiplier = scoreUpdate.Multiplier
                            }
            });
        }

        await this.HttpContext.SendDataAsync(result);
    }

    [Route(HttpVerbs.Post, "/admin/start_tournament_encounter")]
    public async Task PostAdminStartTournamentEncounter(
        [QueryField] int oneID,
        [QueryField] int twoID,
        [QueryField] float duration) {
        var module = Server.Instance.Module;
        var one = module.Connections.Values.FirstOrDefault(x => x.Player?.ID == oneID);
        var two = module.Connections.Values.FirstOrDefault(x => x.Player?.ID == twoID);
        
        if (one?.Player is null || two?.Player is null) {
            this.HttpContext.Response.StatusCode = 400;
            await this.HttpContext.SendStringAsync("Player(s) not found", "text/plain", Encoding.UTF8);
            return;
        }

        if (one.Player.Stage != two.Player.Stage) {
            this.HttpContext.Response.StatusCode = 400;
            await this.HttpContext.SendStringAsync("Stage mismatch", "text/plain", Encoding.UTF8);
            return;
        }

        const EncounterType encounter = EncounterType.ScoreEncounter;
        var length = duration == 0 ? Server.Instance.Config.Encounters.ScoreDuration : duration;

        if (one.EncounterRequests.TryGetValue(encounter, out var value) && value.Contains(two.Player.ID)) {
            one.EncounterRequests[encounter].Remove(two.Player.ID);
        }

        if (two.EncounterRequests.TryGetValue(encounter, out value) && value.Contains(one.Player.ID)) {
            two.EncounterRequests[encounter].Remove(one.Player.ID);
        }

        module.SendToContext(one.Context, new ClientboundEncounterStart {
            PlayerID = two.Player.ID,
            EncounterType = encounter,
            EncounterLength = length
        });

        module.SendToContext(two.Context, new ClientboundEncounterStart {
            PlayerID = one.Player.ID,
            EncounterType = encounter,
            EncounterLength = length
        });

        await this.HttpContext.SendStringAsync("OK", "text/plain", Encoding.UTF8);
    }
}
