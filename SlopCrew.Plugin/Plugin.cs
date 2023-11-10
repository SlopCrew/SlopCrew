using BepInEx;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SlopCrew.API;

namespace SlopCrew.Plugin;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInProcess("Bomb Rush Cyberfunk.exe")]
public class Plugin : BaseUnityPlugin {
    private IHost host = null!;

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

            services.AddSingleton<SlopCrewAPI>();
        });

        this.host = builder.Build();
        APIManager.RegisterAPI(this.host.Services.GetRequiredService<SlopCrewAPI>());
        this.host.Start();
    }

    private void OnDestroy() {
        this.host.StopAsync().GetAwaiter().GetResult();
        this.host.Dispose();
    }
}
