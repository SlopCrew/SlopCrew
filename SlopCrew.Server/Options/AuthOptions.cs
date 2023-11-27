namespace SlopCrew.Server.Options; 

public class AuthOptions {
    public string DiscordClientId { get; set; } = string.Empty;
    public string DiscordClientSecret { get; set; } = string.Empty;
    public string DiscordRedirectUri { get; set; } = string.Empty;
    public string AdminSecret { get; set; } = string.Empty;
}
