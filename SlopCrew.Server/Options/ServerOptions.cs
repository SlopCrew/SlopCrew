namespace SlopCrew.Server.Options; 

public class ServerOptions {
    public ushort Port { get; set; } = 42069;
    public int TickRate { get; set; } = 10;
    public bool QuieterLogs { get; set; } = false;
}
