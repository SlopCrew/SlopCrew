using Microsoft.EntityFrameworkCore;

// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

namespace SlopCrew.Server.Database;

[PrimaryKey("DiscordId")]
public class User {
    public required string DiscordId { get; set; }
    public required string DiscordUsername { get; set; }
    public required string DiscordToken { get; set; }
    public required string DiscordRefreshToken { get; set; }
    public DateTime DiscordTokenExpires { get; set; }
    public string? DiscordAvatar { get; set; }

    public string? GameToken { get; set; } = null;
    public bool IsCommunityContributor { get; set; } = false;

    public List<Crew> Crews { get; set; } = new();
    public List<Crew> OwnedCrews { get; set; } = new();

    public Crew? RepresentingCrew { get; set; } = null;
    public string? RepresentingCrewId { get; set; } = null;
}
