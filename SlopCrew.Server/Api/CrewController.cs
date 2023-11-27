using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SlopCrew.Server.Database;

namespace SlopCrew.Server.Api;

[ApiController]
[Route("api/[controller]")]
public class CrewController(
    UserService userService,
    CrewService crewService,
    ILogger<AuthController> logger
) : ControllerBase {
    public class CreateRequest {
        [JsonPropertyName("name")] public required string Name { get; set; }
        [JsonPropertyName("tag")] public required string Tag { get; set; }
    }

    public class CreateResponse {
        [JsonPropertyName("id")] public required string Id { get; set; }
    }

    public class SimpleCrewResponse {
        [JsonPropertyName("id")] public required string Id { get; set; }
        [JsonPropertyName("name")] public required string Name { get; set; }
        [JsonPropertyName("tag")] public required string Tag { get; set; }
    }

    public class CrewResponse : SimpleCrewResponse {
        [JsonPropertyName("members")] public required List<CrewMember> Members { get; set; }
    }

    public class CrewMember {
        [JsonPropertyName("id")] public required string Id { get; set; }
        [JsonPropertyName("username")] public required string Username { get; set; }
        [JsonPropertyName("owner")] public required bool Owner { get; set; }
        [JsonPropertyName("avatar")] public string? Avatar { get; set; }
    }

    [HttpPost("create")]
    [Authorize]
    public async Task<ActionResult<CreateResponse>> PostCreate(CreateRequest req) {
        var user = await userService.GetUserFromIdentity(this.User);
        if (user is null) return this.Unauthorized();

        if (!crewService.CanJoinOrCreateCrew(user)) {
            return this.BadRequest("You cannot create any more crews");
        }

        try {
            var crew = await crewService.CreateCrew(user, req.Name, req.Tag);
            return new CreateResponse {
                Id = crew.Id
            };
        } catch (Exception e) {
            logger.LogError(e, "Error creating crew");
            return this.BadRequest();
        }
    }

    [HttpGet("crews")]
    public async Task<ActionResult<List<SimpleCrewResponse>>> GetCrews() {
        var user = await userService.GetUserFromIdentity(this.User);
        if (user is null) return this.Unauthorized();

        var crews = await crewService.GetCrews(user);
        return crews.Select(c => new SimpleCrewResponse {
            Id = c.Id,
            Name = c.Name,
            Tag = c.Tag
        }).ToList();
    }

    [HttpGet("crew/{crewId}")]
    public async Task<ActionResult<CrewResponse>> GetCrew([FromRoute] string crewId) {
        var user = await userService.GetUserFromIdentity(this.User);
        if (user is null) return this.Unauthorized();

        var crew = await crewService.GetCrew(crewId);
        if (crew is null) return this.NotFound();
        if (!crew.Members.Contains(user)) return this.NotFound();

        return new CrewResponse {
            Id = crew.Id,
            Name = crew.Name,
            Tag = crew.Tag,
            Members = crew.Members.Select(m => new CrewMember {
                Id = m.DiscordId,
                Username = m.DiscordUsername,
                Owner = crew.Owners.Contains(m),
                Avatar = m.DiscordAvatar
            }).ToList()
        };
    }
}
