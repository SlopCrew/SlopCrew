namespace SlopCrew.Server.XmasEvent;

public static class XmasConstants {
    public const bool Enabled = true;
    public const int GiftCooldownInSeconds = 30;
    public const int StateBroadcastCooldownInSeconds = 1;
    
    // ID of stages which receive event state updates
    public static readonly int[] BroadcastStateToStages = [
        11 // Square
    ];

    // Give us wiggle room w/ 7 phases even though tree only has 5. Worst case, the last 2 phases never happen, it's
    // a few extra bytes transmitted to clients.
    public const int EventPhaseCount = 7;

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
