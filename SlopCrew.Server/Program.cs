using Microsoft.Extensions.Options;
using SlopCrew.Server;
using SlopCrew.Server.Options;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders().AddConsole();

void BindConfig<T>(string name) where T : class, new() {
    builder.Services.Configure<T>(builder.Configuration.GetSection(name));
}

BindConfig<ServerOptions>("Server");
BindConfig<GraphiteOptions>("Graphite");

// This is fucking stupid. I hate MSDI
builder.Services.AddSingleton<NetworkService>();
builder.Services.AddHostedService<NetworkService>(p => p.GetRequiredService<NetworkService>());

builder.Services.AddTransient<NetworkClient>();
builder.Services.AddSingleton<MetricsService>();
builder.Services.AddSingleton<TickRateService>();

var app = builder.Build();
app.Run();
