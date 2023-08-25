using Serilog;
using Serilog.Core;
using SlopCrew.Common.Network.Clientbound;
using System.Net.Security;
using WebSocketSharp.Server;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using System.IO;
namespace SlopCrew.Server;


public class Server {
    public static Server Instance = new();
    public static Logger Logger = null!;

    private string interfaceStr;
    private string certPath;
    private WebSocketServer wsServer;

    public Server() {
        this.interfaceStr = Environment.GetEnvironmentVariable("SLOP_INTERFACE") ?? "ws://0.0.0.0:42069";
        var debug = Environment.GetEnvironmentVariable("SLOP_DEBUG") == "true";

        // Check if cert exists, we need 2 paths here incase we're in Docker land
        if (Console.IsInputRedirected) {
            this.certPath = "/cert/cert.pfx";
        } else {
            this.certPath = "cert/cert.pfx";
        }

        this.wsServer = new WebSocketServer(this.interfaceStr);
        if (File.Exists(this.certPath) && this.interfaceStr.StartsWith("wss")) {
            this.wsServer.SslConfiguration.ServerCertificate = new X509Certificate2(this.certPath);
            this.wsServer.SslConfiguration.EnabledSslProtocols = SslProtocols.Tls12;
            this.wsServer.SslConfiguration.ClientCertificateRequired = false;
            this.wsServer.SslConfiguration.CheckCertificateRevocation = false;
        }
        
        this.wsServer.AddWebSocketService<ServerConnection>("/");

        var logger = new LoggerConfiguration().WriteTo.Console();
        if (debug) logger = logger.MinimumLevel.Verbose();
        Logger = logger.CreateLogger();
        Log.Logger = Logger;
    }

    public void Start() {
        this.wsServer.Start();
        if (File.Exists(this.certPath) && this.interfaceStr.StartsWith("wss")) {
            Log.Information("Starting with SSL");
            Log.Information("Listening on {Interface} - press any key to close", this.interfaceStr);
        } else {
            Log.Information("Starting without SSL, make sure your cert.pfx is in the cert dir and interface starts with wss");
            Log.Information("Listening on {Interface} - press any key to close", this.interfaceStr);
        }
        
    
        if (Console.IsInputRedirected) {
            // If running in a non-interactive environment (like Docker), just wait indefinitely
            while (true) {
                System.Threading.Thread.Sleep(10000);
            }
        } else {
            Console.ReadKey();
            this.wsServer.Stop();
        }
    }


    public void TrackConnection(ServerConnection conn) {
        var player = conn.Player;
        if (player is null) {
            Log.Warning("TrackConnection but player is null? {Connection}", conn.DebugName());
            return;
        }

        // Remove from the old stage if crossing into a new one
        if (conn.LastStage != null && conn.LastStage != player.Stage) {
            this.BroadcastNewPlayers(conn.LastStage.Value);
        }

        // ...and broadcast into the new one
        this.BroadcastNewPlayers(player.Stage);
    }

    public void UntrackConnection(ServerConnection conn) {
        var player = conn.Player;

        // Don't bother untracking someone we never tracked in the first place
        if (player is null) return;

        // Contains checks just in case we get into this state somehow
        this.BroadcastNewPlayers(player.Stage, conn);
    }

    private void BroadcastNewPlayers(int stage, ServerConnection? exclude = null) {
        var connections = this.GetConnections()
                              .Where(x => x.Player?.Stage == stage)
                              .ToList();

        foreach (var connection in connections) {
            var players = connections
                          .Where(x =>
                                     x.Player?.ID != connection.Player?.ID
                                     && x.ID != exclude?.ID)
                          .Select(c => c.Player)
                          .ToList();

            connection.Send(new ClientboundPlayersUpdate {
                Players = players!
            });
        }
    }

    public uint GetNextID() {
        var ids = this.GetConnections().Select(x => x.Player?.ID).ToList();
        var id = 0u;
        while (ids.Contains(id)) id++;
        return id;
    }

    public List<ServerConnection> GetConnections() {
        var service = this.wsServer.WebSocketServices["/"];
        return service.Sessions.Sessions
                      .Cast<ServerConnection>()
                      .ToList();
    }
}
