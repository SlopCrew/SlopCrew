using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using SlopCrew.Server.Database;

namespace SlopCrew.Server.Api;

[ApiController]
[Route("api/[controller]")]
public class MetricsController(MetricsService metricsService, ILogger<MetricsController> logger) : ControllerBase {
    public class MetricsResponse {
        [JsonPropertyName("connections")] public required int Connections { get; set; }
        [JsonPropertyName("population")] public required Dictionary<int, int> Population { get; set; }
    }

    // GET /api/metrics
    [HttpGet]
    public Task<ActionResult<MetricsResponse>> Get()
        => Task.FromResult<ActionResult<MetricsResponse>>(new MetricsResponse {
            Connections = metricsService.Connections,
            Population = metricsService.Population
        });
}
