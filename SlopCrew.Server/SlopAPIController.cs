using System.Text;
using System.Text.Json;
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

        var json = JsonSerializer.Serialize(response);
        await this.HttpContext.SendStringAsync(json, "application/json", Encoding.UTF8);
    }

    [Route(HttpVerbs.Get, "/shields")]
    public async Task GetShields() {
        var response = new {
            schemaVersion = 1,
            label = "players online",
            message = Server.Instance.Metrics.Connections.ToString(),
            color = "orange"
        };

        var json = JsonSerializer.Serialize(response);
        Console.WriteLine(json);
        await this.HttpContext.SendStringAsync(json, "application/json", Encoding.UTF8);
    }
}
