using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SlopCrew.Server.Database;
using SlopCrew.Server.Options;

namespace SlopCrew.Server.Api;

[ApiController]
[Route("api/[controller]")]
public class AdminController(
    UserService userService,
    NetworkService networkService,
    ILogger<AdminController> logger,
    IOptions<AuthOptions> options
)
    : ControllerBase {
    public class ClientResponse {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("id")] public uint? Id { get; set; }
        [JsonPropertyName("key")] public string? Key { get; set; }
        [JsonPropertyName("ip")] public required string Ip { get; set; }
    }

    [HttpPost("make_community_contributor")]
    public async Task<StatusCodeResult> PostMakeCommunityContributor([FromQuery] string id) {
        if (!this.IsAdmin()) return this.Unauthorized();
        if (!await userService.MakeCommunityContributor(id)) return this.NotFound();
        return this.NoContent();
    }

    [HttpGet("clients")]
    public ActionResult<List<ClientResponse>> GetClients() {
        if (!this.IsAdmin()) return this.Unauthorized();
        return this.Ok(networkService.Clients.Select(x => new ClientResponse {
            Name = x.Player?.Name,
            Id = x.Player?.Id,
            Ip = x.Ip,
            Key = x.Key
        }));
    }

    // This is bad but I cbf to google the "right way"
    private bool IsAdmin() {
        var auth = this.Request.Headers["Authorization"];
        var secret = options.Value.AdminSecret;
        if (auth.Count != 1) return false;
        if (string.IsNullOrEmpty(secret)) return false;
        return auth[0] == options.Value.AdminSecret;
    }
}
