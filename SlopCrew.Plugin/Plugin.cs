using System;
using System.Threading;
using BepInEx;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SlopCrew.Plugin;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInProcess("Bomb Rush Cyberfunk.exe")]
public class Plugin : BaseUnityPlugin {
    private IHost host = null!;

    private void Awake() {
        var builder = new HostBuilder();
        builder.ConfigureServices((hostContext, services) => {
            services.AddSingleton(this.Logger);
            services.AddHostedService<SlopConnectionManager>();
        });

        this.host = builder.Build();
        this.host.Start();
    }

    private void OnDestroy() {
        this.host.StopAsync().GetAwaiter().GetResult();
        this.host.Dispose();
    }
}
