using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using SlopCrew.Common.Proto;

namespace SlopCrew.Plugin;

public class ServerConfig : IHostedService {
    public ClientboundHello? Hello;
    private ConnectionManager connectionManager;

    public ServerConfig(ConnectionManager connectionManager) {
        this.connectionManager = connectionManager;
    }
    
    public Task StartAsync(CancellationToken cancellationToken) {
        this.connectionManager.MessageReceived += this.MessageReceived;
        this.connectionManager.Disconnected += this.Disconnected;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) {
        this.connectionManager.MessageReceived -= this.MessageReceived;
        this.connectionManager.Disconnected -= this.Disconnected;
        return Task.CompletedTask;
    }

    private void Disconnected() {
        this.Hello = null;
    }
    
    private void MessageReceived(ClientboundMessage message) {
        if (message.MessageCase is ClientboundMessage.MessageOneofCase.Hello) {
            this.Hello = message.Hello;
        }
    }
}
