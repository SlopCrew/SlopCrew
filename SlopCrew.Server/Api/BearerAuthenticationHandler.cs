using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using SlopCrew.Server.Database;

namespace SlopCrew.Server.Api;

public class BearerAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ISystemClock clock,
    UserService userService
) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder, clock) {
    protected override Task<AuthenticateResult> HandleAuthenticateAsync() {
        var authHeader = this.Request.Headers["Authorization"].FirstOrDefault();
        if (authHeader == null) return Task.FromResult(AuthenticateResult.NoResult());

        var user = userService.GetUserByKey(authHeader);
        if (user == null) return Task.FromResult(AuthenticateResult.Fail("Invalid key"));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.Name, user.DiscordUsername),
            new Claim(ClaimTypes.NameIdentifier, user.DiscordId)
        }, "Bearer"));
        
        var ticket = new AuthenticationTicket(principal, "Bearer");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
