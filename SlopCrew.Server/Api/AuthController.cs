using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SlopCrew.Server.Database;

namespace SlopCrew.Server.Api;

[ApiController]
[Route("api/[controller]")]
public class AuthController(UserService userService, ILogger<AuthController> logger) : ControllerBase {
    public class LoginRequest {
        [JsonPropertyName("code")] public required string Code { get; set; }
    }

    public class LoginResponse : MeResponse {
        [JsonPropertyName("key")] public required string Key { get; set; }
    }

    public class MeResponse {
        [JsonPropertyName("username")] public required string Username { get; set; }
        [JsonPropertyName("id")] public required string Id { get; set; }
        [JsonPropertyName("avatar")] public string? Avatar { get; set; }
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> PostLogin(LoginRequest req) {
        try {
            var user = await userService.ExchangeCodeForUser(req.Code);
            var gameToken = await userService.RegenerateGameToken(user);

            return new LoginResponse {
                Username = user.DiscordUsername,
                Id = user.DiscordId,
                Avatar = user.DiscordAvatar,
                Key = gameToken
            };
        } catch (Exception e) {
            logger.LogError(e, "Error exchanging code for user");
            return this.BadRequest();
        }
    }
    
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<MeResponse>> GetMe() {
        var user = await userService.GetUserFromIdentity(this.User);
        if (user is null) return this.Unauthorized();
        
        var me = await userService.GetDiscordMeResponse(user);
        return new MeResponse {
            Username = me.Username,
            Id = me.Id,
            Avatar = me.Avatar
        };
    }
}
