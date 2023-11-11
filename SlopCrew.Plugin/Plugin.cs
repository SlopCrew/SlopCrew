using BepInEx;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SlopCrew.API;

namespace SlopCrew.Plugin;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInProcess("Bomb Rush Cyberfunk.exe")]
public class Plugin : BaseUnityPlugin {
    public static IHost Host = null!;

    private void Awake() {
        var builder = new HostBuilder();
        builder.ConfigureServices((hostContext, services) => {
            services.AddSingleton(this.Logger);

            void AddSingletonHostedService<T>() where T : class, IHostedService {
                services.AddSingleton<T>();
                services.AddHostedService<T>(p => p.GetRequiredService<T>());
            }

            AddSingletonHostedService<SlopConnectionManager>();
            AddSingletonHostedService<LocalPlayerManager>();
            AddSingletonHostedService<PlayerManager>();
            AddSingletonHostedService<PatchManager>();

            services.AddSingleton<SlopCrewAPI>();
        });

        Host = builder.Build();
        APIManager.RegisterAPI(Host.Services.GetRequiredService<SlopCrewAPI>());
        Host.Start();
    }

    private void OnDestroy() {
        Host.StopAsync().GetAwaiter().GetResult();
        Host.Dispose();
    }
}
