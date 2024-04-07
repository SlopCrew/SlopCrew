using System.Threading.Channels;
using System.Timers;
using Microsoft.Extensions.Options;
using SlopCrew.Server.Options;
using Timer = System.Timers.Timer;

namespace SlopCrew.Server;

public class TickRateService : IDisposable {
    private ILogger<TickRateService> logger;
    private ServerOptions serverOptions;

    private Task task;
    private CancellationTokenSource cts;

    public ulong CurrentTick;
    public int TickRate;

    public event Action? Tick;

    public TickRateService(ILogger<TickRateService> logger, IOptions<ServerOptions> serverOptions) {
        this.logger = logger;
        this.serverOptions = serverOptions.Value;

        this.cts = new CancellationTokenSource();
        this.task = Task.Run(async () => {
            // Write nulls into a channel from an interval timer.
            // Channel capacity is limited to one second of ticks, so
            // during prolonged high CPU, ticks are dropped instead of
            // accumulating a large backlog.
            var channel = Channel.CreateBounded<object>(this.serverOptions.TickRate);
            var timer = new Timer(1000 / this.serverOptions.TickRate);
            timer.Elapsed += OnTimerElapsed;
            timer.AutoReset = true;
            timer.Enabled = true;

            void OnTimerElapsed(Object? source, ElapsedEventArgs e) {
                // called on any thread, but channels are thread-safe
                channel.Writer.TryWrite(null);
            }

            while (!this.cts.IsCancellationRequested) {
                await channel.Reader.ReadAsync();
                try {
                    this.CurrentTick++;
                    this.RunTick();
                } catch (Exception e) {
                    this.logger.LogError(e, "Error running tick");
                }
            }
            timer.Dispose();
        }, this.cts.Token);

        this.TickRate = this.serverOptions.TickRate;
    }

    public void Dispose() {
        this.cts.Cancel();
        this.task.Wait();
    }

    private void RunTick() {
        this.Tick?.Invoke();
    }
}
