using System.Reflection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using SlopCrew.Server;
using SlopCrew.Server.Api;
using SlopCrew.Server.Database;
using SlopCrew.Server.Encounters;
using SlopCrew.Server.Options;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders().AddConsole();

var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();

var appsettings = Path.Combine(
    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty,
    "appsettings.json");
if (File.Exists(appsettings)) builder.Configuration.AddJsonFile(appsettings);

T BindConfig<T>(string name) where T : class, new() {
    var config = builder.Configuration.GetSection(name);
    builder.Services.Configure<T>(config);

    var instance = new T();
    config.Bind(instance);
    return instance;
}

var serverOptions = BindConfig<ServerOptions>("Server");
BindConfig<GraphiteOptions>("Graphite");
BindConfig<EncounterOptions>("Encounter");
var databaseOptions = BindConfig<DatabaseOptions>("Database");
var authOptions = BindConfig<AuthOptions>("Auth");

if (serverOptions.QuieterLogs) {
    builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
}

void AddSingletonHostedService<T>() where T : class, IHostedService {
    builder.Services.AddSingleton<T>();
    builder.Services.AddHostedService<T>(p => p.GetRequiredService<T>());
}

AddSingletonHostedService<NetworkService>();
AddSingletonHostedService<RaceConfigService>();
AddSingletonHostedService<DiscordRefreshService>();

builder.Services.AddTransient<NetworkClient>();
builder.Services.AddSingleton<MetricsService>();
builder.Services.AddSingleton<TickRateService>();
builder.Services.AddSingleton<EncounterService>();

builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<CrewService>();

builder.Services.AddDbContext<SlopDbContext>(
    options => options.UseSqlite($"DataSource={databaseOptions.DatabasePath}"));

builder.Services.AddControllers();

builder.Services.AddAuthentication("Bearer")
    .AddScheme<AuthenticationSchemeOptions, BearerAuthenticationHandler>("Bearer", _ => { });

var redirectUri = authOptions.DiscordRedirectUri;
if (!string.IsNullOrEmpty(redirectUri)) {
    builder.Services.AddCors(options => {
        var domain = new Uri(redirectUri).GetLeftPart(UriPartial.Authority);
        options.AddDefaultPolicy(corsBuilder => corsBuilder
                                     .WithOrigins(domain)
                                     .WithHeaders("Content-Type", "Authorization")
                                     .WithMethods("DELETE", "PATCH"));
    });
}

var app = builder.Build();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

logger.LogInformation("Migrating database...");
var context = app.Services.GetRequiredService<SlopDbContext>();
context.Database.Migrate();

app.Run();
