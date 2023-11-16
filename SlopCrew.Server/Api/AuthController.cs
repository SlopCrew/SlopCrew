using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using SlopCrew.Server.Database;

namespace SlopCrew.Server.Api;

[ApiController]
[Route("api/[controller]")]
public class AuthController(UserService userService, ILogger<AuthController> logger) : ControllerBase {
    public class LoginRequest {
        [JsonPropertyName("code")] public required string Code { get; set; }
    }

    public class LoginResponse {
        [JsonPropertyName("username")] public required string Username { get; set; }
        [JsonPropertyName("id")] public required string Id { get; set; }
        [JsonPropertyName("key")] public required string Key { get; set; }
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> PostLogin(LoginRequest req) {
        try {
            var user = await userService.ExchangeCodeForUser(req.Code);
            var gameToken = await userService.RegenerateGameToken(user);

            return new LoginResponse {
                Username = user.DiscordUsername,
                Id = user.DiscordId,
                Key = gameToken
            };
        } catch (Exception e) {
            logger.LogError(e, "Error exchanging code for user");
            return this.BadRequest();
        }
    }
}
