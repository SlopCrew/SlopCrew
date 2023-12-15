namespace SlopCrew.Server.XmasEvent;

public static class XmasConstants {
    public const bool Enabled = true;
    public const int GiftCooldownInSeconds = 30;
    // Reserved player ID for custom packets sent from the server.
    public const uint ServerPlayerID = uint.MaxValue;
    // Peeps running the xmas event, to have control over things.
    public static readonly HashSet<string> SlopBrewDiscordIDs = [
        // Garuda
        "89046073665912832",
        // Lazy Duchess
        "167668839323009024",
        // Muppo
        "307265029969805313",
        // Cspot
        "125351090790203392"
    ];
}
