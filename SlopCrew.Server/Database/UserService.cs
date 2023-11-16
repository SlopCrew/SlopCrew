using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SlopCrew.Server.Options;

namespace SlopCrew.Server.Database;

public class UserService(IOptions<AuthOptions> options, SlopDbContext dbContext) {
    private HttpClient client = new();

    public class DiscordTokenExchangeResponse {
        [JsonPropertyName("access_token")] public required string AccessToken { get; set; }
        [JsonPropertyName("token_type")] public required string TokenType { get; set; }
        [JsonPropertyName("expires_in")] public required int ExpiresIn { get; set; }
        [JsonPropertyName("refresh_token")] public required string RefreshToken { get; set; }
        [JsonPropertyName("scope")] public required string Scope { get; set; }
    }

    public class DiscordMeResponse {
        [JsonPropertyName("id")] public required string Id { get; set; }
        [JsonPropertyName("username")] public required string Username { get; set; }
        [JsonPropertyName("discriminator")] public required string Discriminator { get; set; }
    }

    public async Task<User> ExchangeCodeForUser(string code) {
        var exchangeRequestUri = new HttpRequestMessage(HttpMethod.Post, "https://discord.com/api/oauth2/token");
        exchangeRequestUri.Content = new FormUrlEncodedContent(new Dictionary<string, string> {
            {"client_id", options.Value.DiscordClientId},
            {"client_secret", options.Value.DiscordClientSecret},
            {"grant_type", "authorization_code"},
            {"code", code},
            {"redirect_uri", options.Value.DiscordRedirectUri}
        });

        var exchangeRequest = await client.SendAsync(exchangeRequestUri);
        exchangeRequest.EnsureSuccessStatusCode();
        var exchangeResponse = await exchangeRequest.Content.ReadFromJsonAsync<DiscordTokenExchangeResponse>();
        if (exchangeResponse is null) throw new Exception("Error exchanging code for token");

        var meRequestUri = new HttpRequestMessage(HttpMethod.Get, "https://discord.com/api/users/@me");
        meRequestUri.Headers.Authorization = new AuthenticationHeaderValue("Bearer", exchangeResponse.AccessToken);

        var meRequest = await client.SendAsync(meRequestUri);
        meRequest.EnsureSuccessStatusCode();
        var meResponse = await meRequest.Content.ReadFromJsonAsync<DiscordMeResponse>();
        if (meResponse is null) throw new Exception("Error fetching user info");

        var user = await dbContext.Users.FindAsync(meResponse.Id);
        if (user is not null) {
            user.DiscordToken = exchangeResponse.AccessToken;
            user.DiscordRefreshToken = exchangeResponse.RefreshToken;
            user.DiscordTokenExpires = DateTime.UtcNow.AddSeconds(exchangeResponse.ExpiresIn);
            await dbContext.SaveChangesAsync();
            return user;
        } else {
            var username = $"@{meResponse.Username}";
            if (meResponse.Discriminator != "0") {
                username = $"{meResponse.Username}#{meResponse.Discriminator}";
            }

            var newUser = new User {
                DiscordId = meResponse.Id,
                DiscordUsername = username,
                DiscordToken = exchangeResponse.AccessToken,
                DiscordRefreshToken = exchangeResponse.RefreshToken,
                DiscordTokenExpires = DateTime.UtcNow.AddSeconds(exchangeResponse.ExpiresIn)
            };

            await dbContext.Users.AddAsync(newUser);
            await dbContext.SaveChangesAsync();
            return newUser;
        }
    }

    public async Task<string> RegenerateGameToken(User user) {
        // Dear UUID spec:
        // "Do not assume that UUIDs are hard to guess; they should not be used as security capabilities
        // (identifiers whose mere possession grants access), for example."
        // I don't care.
        var token = Guid.NewGuid().ToString();
        user.GameToken = token;
        await dbContext.SaveChangesAsync();
        return token;
    }

    public async Task RefreshDiscordToken(User user) {
        var refreshRequestUri = new HttpRequestMessage(HttpMethod.Post, "https://discord.com/api/oauth2/token");
        refreshRequestUri.Content = new FormUrlEncodedContent(new Dictionary<string, string> {
            {"client_id", options.Value.DiscordClientId},
            {"client_secret", options.Value.DiscordClientSecret},
            {"grant_type", "refresh_token"},
            {"refresh_token", user.DiscordRefreshToken},
            {"redirect_uri", options.Value.DiscordRedirectUri}
        });

        var refreshRequest = await client.SendAsync(refreshRequestUri);
        refreshRequest.EnsureSuccessStatusCode();
        var refreshResponse = await refreshRequest.Content.ReadFromJsonAsync<DiscordTokenExchangeResponse>();
        if (refreshResponse is null) throw new Exception("Error refreshing token");

        user.DiscordToken = refreshResponse.AccessToken;
        user.DiscordRefreshToken = refreshResponse.RefreshToken;
        user.DiscordTokenExpires = DateTime.UtcNow.AddSeconds(refreshResponse.ExpiresIn);

        await dbContext.SaveChangesAsync();
    }

    public async Task MakeCommunityContributor(string id) {
        var user = await GetUserById(id);
        if (user is not null) {
            user.IsCommunityContributor = true;
            await dbContext.SaveChangesAsync();
        }
    }

    public Task<User?> GetUserById(string id)
        => dbContext.Users.FirstOrDefaultAsync(u => u.DiscordId == id);

    public User? GetUserByKey(string key)
        => dbContext.Users.FirstOrDefault(u => u.GameToken == key);
}
