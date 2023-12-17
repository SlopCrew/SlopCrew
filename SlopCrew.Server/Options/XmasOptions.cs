namespace SlopCrew.Server.Options; 

public class XmasOptions {
    // Xmas event state is persisted to disk at this path on the filesystem.
    public string StatePath { get; set; } = "xmas-event-state.json";
}
