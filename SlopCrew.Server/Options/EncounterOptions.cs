namespace SlopCrew.Server.Options;

public class EncounterOptions {
    public List<string> BannedPlugins { get; set; } = new();
    public uint ScoreBattleLength { get; set; } = 180;
    public uint ComboBattleLength { get; set; } = 300;
    public uint ComboBattleGrace { get; set; } = 15;
    public string? RaceConfigDirectory { get; set; } = null;
}
