namespace SlopCrew.Server.Options; 

public class AuthOptions {
    public string JwtSecret { get; set; } = string.Empty;
    public string DiscordClientId { get; set; } = string.Empty;
    public string DiscordClientSecret { get; set; } = string.Empty;
    public string DiscordRedirectUri { get; set; } = string.Empty;
}
