using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using Microsoft.Extensions.Hosting;

namespace SlopCrew.Plugin;

public class PatchManager : IHostedService {
    private Harmony harmony = new("SlopCrew.Plugin.Harmony");

    public Task StartAsync(CancellationToken cancellationToken) {
        var patches = typeof(Plugin).Assembly.GetTypes()
            .Where(m => m.GetCustomAttributes(typeof(HarmonyPatch), false).Length > 0)
            .ToArray();

        foreach (var patch in patches) {
            this.harmony.PatchAll(patch);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) {
        this.harmony.UnpatchSelf();
        return Task.CompletedTask;
    }
}
