namespace  SlopCrew.Server;

public class TickRateService : IDisposable {
    private ILogger<TickRateService> logger;
    private Task task;
    private CancellationTokenSource cts;

    public event Action? Tick;
    
    public TickRateService(ILogger<TickRateService> logger) {
        this.logger = logger;
        const int tickRate = 10; // TODO
        
        this.cts = new CancellationTokenSource();
        this.task = Task.Run(async () => {
            while (!this.cts.IsCancellationRequested) {
                await Task.Delay(1000 / tickRate);
                try {
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

