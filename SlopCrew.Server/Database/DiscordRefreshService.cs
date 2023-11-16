using System.Timers;
using Timer = System.Timers.Timer;

namespace SlopCrew.Server.Database;

public class DiscordRefreshService(
    SlopDbContext dbContext,
    UserService userService,
    ILogger<DiscordRefreshService> logger
) : IHostedService {
    private const int RefreshInterval = 60 * 60 * 1000;  // 1 hour
    private const int ExpiryLimit = 60 * 60 * 24 * 1000; // 1 day
    private Timer timer;

    public Task StartAsync(CancellationToken cancellationToken) {
        this.timer = new Timer(RefreshInterval);
        this.timer.Elapsed += this.TimerElapsed;
        this.timer.Start();
        return Task.CompletedTask;
    }

    private async void TimerElapsed(object? sender, ElapsedEventArgs args) {
        var users = dbContext.Users
            .Where(x => x.DiscordTokenExpires < DateTime.UtcNow.AddMilliseconds(ExpiryLimit));

        foreach (var user in users) {
            try {
                await userService.RefreshDiscordToken(user);
            } catch (Exception e) {
                logger.LogError(e, "Error refreshing token for user {UserId}", user.DiscordId);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) {
        this.timer.Stop();
        this.timer.Dispose();
        return Task.CompletedTask;
    }
}
