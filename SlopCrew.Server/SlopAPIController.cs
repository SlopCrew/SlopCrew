using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;

namespace SlopCrew.Server;

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
}
