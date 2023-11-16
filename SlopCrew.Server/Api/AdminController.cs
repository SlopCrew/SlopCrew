using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SlopCrew.Server.Database;
using SlopCrew.Server.Options;

namespace SlopCrew.Server.Api;

[ApiController]
[Route("api/[controller]")]
public class AdminController(UserService userService, ILogger<AdminController> logger, IOptions<AuthOptions> options)
    : ControllerBase {
    [HttpPost("make_community_contributor")]
    public async Task<StatusCodeResult> PostMakeCommunityContributor([FromQuery] string id) {
        if (!this.IsAdmin()) return this.Unauthorized();
        await userService.MakeCommunityContributor(id);
        return this.NoContent();
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
