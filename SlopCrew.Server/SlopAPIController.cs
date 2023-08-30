using System.Text;
using System.Text.Json;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using Serilog;

namespace SlopCrew.Server;

public class SlopAPIController : WebApiController {
    [Route(HttpVerbs.Get, "/metrics")]
    public async Task GetMetrics() {
        var metrics = Server.Instance.Metrics;
        var response = new {
            connections = metrics.Connections,
            population = metrics.Population
        };

        var json = JsonSerializer.Serialize(response);
        await this.HttpContext.SendStringAsync(json, "application/json", Encoding.UTF8);
    }
}
