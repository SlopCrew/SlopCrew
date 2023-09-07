using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;

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
}
