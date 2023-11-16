using Microsoft.Extensions.Options;
using SlopCrew.Server.Options;

namespace SlopCrew.Server;

public class TickRateService : IDisposable {
    private ILogger<TickRateService> logger;
    private ServerOptions serverOptions;
    
    private Task task;
    private CancellationTokenSource cts;

    public ulong CurrentTick;
    public event Action? Tick;

    public TickRateService(ILogger<TickRateService> logger, IOptions<ServerOptions> serverOptions) {
        this.logger = logger;
        this.serverOptions = serverOptions.Value;

        this.cts = new CancellationTokenSource();
        this.task = Task.Run(async () => {
            while (!this.cts.IsCancellationRequested) {
                await Task.Delay(1000 / this.serverOptions.TickRate);
                try {
                    this.CurrentTick++;
                    this.RunTick();
                } catch (Exception e) {
                    this.logger.LogError(e, "Error running tick");
                }
            }
        }, this.cts.Token);
    }

    public void Dispose() {
        this.cts.Cancel();
        this.task.Wait();
    }

    private void RunTick() {
        this.Tick?.Invoke();
    }
}
