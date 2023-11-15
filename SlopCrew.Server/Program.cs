using SlopCrew.Server;
using SlopCrew.Server.Encounters;
using SlopCrew.Server.Options;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders().AddConsole();

void BindConfig<T>(string name) where T : class, new() {
    builder.Services.Configure<T>(builder.Configuration.GetSection(name));
}

BindConfig<ServerOptions>("Server");
BindConfig<GraphiteOptions>("Graphite");
BindConfig<EncounterOptions>("Encounter");

// This is fucking stupid. I hate MSDI
void AddSingletonHostedService<T>() where T : class, IHostedService {
    builder.Services.AddSingleton<T>();
    builder.Services.AddHostedService<T>(p => p.GetRequiredService<T>());
}

AddSingletonHostedService<NetworkService>();
AddSingletonHostedService<RaceConfigService>();

builder.Services.AddTransient<NetworkClient>();
builder.Services.AddSingleton<MetricsService>();
builder.Services.AddSingleton<TickRateService>();
builder.Services.AddSingleton<EncounterService>();

var app = builder.Build();
app.Run();
