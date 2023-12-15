using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SlopCrew.Common;
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

    public class SimpleCrewResponse {
        [JsonPropertyName("id")] public required string Id { get; set; }
        [JsonPropertyName("name")] public required string Name { get; set; }
        [JsonPropertyName("tag")] public required string Tag { get; set; }
        [JsonPropertyName("super_owner")] public required string SuperOwner { get; set; }
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
    public async Task<ActionResult<SimpleCrewResponse>> PostCreate(CreateRequest req) {
        var user = await userService.GetUserFromIdentity(this.User);
        if (user is null) return this.Unauthorized();

        if (!crewService.CanJoinOrCreateCrew(user)) {
            return this.BadRequest("You cannot create any more crews");
        }

        if (PlayerNameFilter.HitsFilter(req.Name) || PlayerNameFilter.HitsFilter(req.Tag)) {
            return this.BadRequest("Crew name or tag contains a banned word");
        }

        try {
            var crew = await crewService.CreateCrew(user, req.Name, req.Tag);
            return new SimpleCrewResponse {
                Id = crew.Id,
                Name = crew.Name,
                Tag = crew.Tag,
                SuperOwner = crew.SuperOwner.DiscordId
            };
        } catch (Exception e) {
            logger.LogError(e, "Error creating crew - crew tag may be taken");
            return this.BadRequest();
        }
    }

    [HttpGet("crews")]
    [Authorize]
    public async Task<ActionResult<List<SimpleCrewResponse>>> GetCrews() {
        var user = await userService.GetUserFromIdentity(this.User);
        if (user is null) return this.Unauthorized();

        var crews = await crewService.GetCrews(user);
        return crews.Select(c => new SimpleCrewResponse {
            Id = c.Id,
            Name = c.Name,
            Tag = c.Tag,
            SuperOwner = c.SuperOwner.DiscordId
        }).ToList();
    }

    [HttpGet("{crewId}")]
    [Authorize]
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
            SuperOwner = crew.SuperOwner.DiscordId,
            Members = crew.Members.Select(m => new CrewMember {
                Id = m.DiscordId,
                Username = m.DiscordUsername,
                Owner = crew.Owners.Contains(m),
                Avatar = m.DiscordAvatar
            }).ToList()
        };
    }

    [HttpPatch("{crewId}")]
    [Authorize]
    public async Task<ActionResult> PatchCrew([FromRoute] string crewId, [FromBody] CreateRequest req) {
        var user = await userService.GetUserFromIdentity(this.User);
        if (user is null) return this.Unauthorized();

        if (PlayerNameFilter.HitsFilter(req.Name) || PlayerNameFilter.HitsFilter(req.Tag)) {
            return this.BadRequest("Crew name or tag contains a banned word");
        }

        var crew = await crewService.GetCrew(crewId);
        if (crew is null) return this.NotFound();
        if (!crew.Owners.Contains(user)) return this.Unauthorized();

        try {
            await crewService.UpdateCrew(crew, req.Name, req.Tag);
        } catch (Exception e) {
            logger.LogError(e, "Error updating crew - crew tag may be taken");
            return this.BadRequest();
        }

        return this.NoContent();
    }

    [HttpPost("{crewId}/promote")]
    [Authorize]
    public async Task<ActionResult> PostPromote([FromRoute] string crewId, [FromQuery] string id) {
        var user = await userService.GetUserFromIdentity(this.User);
        if (user is null) return this.Unauthorized();

        var crew = await crewService.GetCrew(crewId);
        if (crew is null) return this.NotFound("Crew not found");
        if (crew.SuperOwner != user) return this.Unauthorized();

        var targetUser = await userService.GetUserById(id);
        if (targetUser is null) return this.NotFound("Target user not found");

        await crewService.PromoteUser(crew, targetUser);
        return this.NoContent();
    }

    [HttpPost("{crewId}/demote")]
    [Authorize]
    public async Task<ActionResult> PostDemote([FromRoute] string crewId, [FromQuery] string id) {
        var user = await userService.GetUserFromIdentity(this.User);
        if (user is null) return this.Unauthorized();

        var crew = await crewService.GetCrew(crewId);
        if (crew is null) return this.NotFound("Crew not found");
        if (crew.SuperOwner != user) return this.Unauthorized();

        var targetUser = await userService.GetUserById(id);
        if (targetUser is null) return this.NotFound("Target user not found");

        if (targetUser.DiscordId == user.DiscordId) {
            return this.BadRequest("Cannot demote yourself");
        }

        if (targetUser == crew.SuperOwner) {
            return this.BadRequest("Cannot demote the super owner");
        }

        var worked = await crewService.DemoteUser(crew, targetUser);
        if (!worked) return this.BadRequest("Cannot demote the last owner of a crew");
        return this.NoContent();
    }

    [HttpGet("{crewId}/invites")]
    [Authorize]
    public async Task<ActionResult<List<string>>> GetInvites([FromRoute] string crewId) {
        var user = await userService.GetUserFromIdentity(this.User);
        if (user is null) return this.Unauthorized();

        var crew = await crewService.GetCrew(crewId);
        if (crew is null) return this.NotFound("Crew not found");
        if (!crew.Owners.Contains(user)) return this.Unauthorized();

        return crew.InviteCodes.ToList();
    }

    [HttpPost("{crewId}/invites")]
    [Authorize]
    public async Task<ActionResult<string>> PostInvite([FromRoute] string crewId) {
        var user = await userService.GetUserFromIdentity(this.User);
        if (user is null) return this.Unauthorized();

        var crew = await crewService.GetCrew(crewId);
        if (crew is null) return this.NotFound("Crew not found");
        if (!crew.Owners.Contains(user)) return this.Unauthorized();

        var code = await crewService.GenerateInviteCode(crew);
        return code;
    }

    [HttpDelete("{crewId}/invites/{code}")]
    [Authorize]
    public async Task<ActionResult> DeleteInvite([FromRoute] string crewId, [FromRoute] string code) {
        var user = await userService.GetUserFromIdentity(this.User);
        if (user is null) return this.Unauthorized();

        var crew = await crewService.GetCrew(crewId);
        if (crew is null) return this.NotFound("Crew not found");
        if (!crew.Owners.Contains(user)) return this.Unauthorized();

        await crewService.DeleteInviteCode(crew, code);
        return this.NoContent();
    }

    [HttpPost("join")]
    [Authorize]
    public async Task<ActionResult<SimpleCrewResponse>> PostJoin([FromQuery] string code) {
        var user = await userService.GetUserFromIdentity(this.User);
        if (user is null) return this.Unauthorized();
        if (!crewService.CanJoinOrCreateCrew(user)) return this.BadRequest("You cannot join any more crews");

        var crew = await crewService.JoinCrew(user, code);
        if (crew is null) return this.BadRequest("Invalid invite code");

        return new SimpleCrewResponse {
            Id = crew.Id,
            Name = crew.Name,
            Tag = crew.Tag,
            SuperOwner = crew.SuperOwner.DiscordId
        };
    }

    [HttpPost("{crewId}/leave")]
    [Authorize]
    public async Task<ActionResult> PostLeave([FromRoute] string crewId) {
        var user = await userService.GetUserFromIdentity(this.User);
        if (user is null) return this.Unauthorized();

        var crew = await crewService.GetCrew(crewId);
        if (crew is null) return this.NotFound("Crew not found");
        if (!crew.Members.Contains(user)) return this.Unauthorized();

        await crewService.LeaveCrew(crew, user);
        return this.NoContent();
    }

    [HttpPost("{crewId}/kick")]
    [Authorize]
    public async Task<ActionResult> PostKick([FromRoute] string crewId, [FromQuery] string id) {
        var user = await userService.GetUserFromIdentity(this.User);
        if (user is null) return this.Unauthorized();

        var crew = await crewService.GetCrew(crewId);
        if (crew is null) return this.NotFound("Crew not found");
        if (!crew.Owners.Contains(user)) return this.Unauthorized();

        var targetUser = await userService.GetUserById(id);
        if (targetUser is null) return this.NotFound("Target user not found");

        await crewService.LeaveCrew(crew, targetUser);
        return this.NoContent();
    }

    [HttpDelete("{crewId}")]
    [Authorize]
    public async Task<ActionResult> DeleteCrew([FromRoute] string crewId) {
        var user = await userService.GetUserFromIdentity(this.User);
        if (user is null) return this.Unauthorized();

        var crew = await crewService.GetCrew(crewId);
        if (crew is null) return this.NotFound("Crew not found");
        if (crew.SuperOwner != user) return this.Unauthorized();

        await crewService.DeleteCrew(crew);
        return this.NoContent();
    }

    [HttpGet("represent")]
    [Authorize]
    public async Task<ActionResult<string>> GetRepresent() {
        var user = await userService.GetUserFromIdentity(this.User);
        if (user is null) return this.Unauthorized();
        if (user.RepresentingCrewId is null) return this.NoContent();
        return user.RepresentingCrewId;
    }

    [HttpPost("represent")]
    [Authorize]
    public async Task<ActionResult> PostRepresent([FromQuery] string? id) {
        var user = await userService.GetUserFromIdentity(this.User);
        if (user is null) return this.Unauthorized();

        if (id is null) {
            await userService.RepresentCrew(user, null);
            return this.NoContent();
        }

        var crew = await crewService.GetCrew(id);
        if (crew is null) return this.NotFound();
        if (!crew.Members.Contains(user)) return this.Unauthorized();

        await userService.RepresentCrew(user, crew);
        return this.NoContent();
    }
}
