using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using SlopCrew.Server.Options;

namespace SlopCrew.Server.Encounters;

public class RaceConfigService(IOptions<EncounterOptions> options, ILogger<RaceConfigService> logger) : IHostedService {
    public List<RaceConfigJson> RaceConfigs = new();

    private JsonSerializerOptions jsonOptions = new() {
        IncludeFields = true
    };

    public Task StartAsync(CancellationToken cancellationToken) {
        if (options.Value.RaceConfigDirectory is not null) {
            logger.LogInformation("Loading race configs from directory");
            var configs = Directory.GetFiles(options.Value.RaceConfigDirectory, "*.json");
            foreach (var config in configs) this.LoadRaceConfig(File.ReadAllText(config));
            return Task.CompletedTask;
        } else {
            logger.LogInformation("Loading race configs from GitHub");
            return this.DownloadFromGitHub();
        }
    }
    
    public bool HasRaceConfigForStage(int stage) => this.RaceConfigs.Any(x => x.Stage == stage);

    public RaceConfigJson GetRaceConfig(int stage) {
        var selection = this.RaceConfigs.Where(x => x.Stage == stage).ToList();
        return selection[new Random().Next(0, selection.Count)];
    }

    public Task StopAsync(CancellationToken cancellationToken) {
        return Task.CompletedTask;
    }

    private async Task DownloadFromGitHub() {
        using var client = new HttpClient();
        var apiUrl =
            $"https://api.github.com/repos/SlopCrew/race-config/contents/?ref=main";
        client.DefaultRequestHeaders.Add("User-Agent", "SlopCrew.Server (https://github.com/SlopCrew/SlopCrew)");
        var response = await client.GetAsync(apiUrl);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var contents = JsonSerializer.Deserialize<List<GitHubDirectoryContents>>(content, this.jsonOptions)!;

        foreach (var file in contents) {
            if (file.Type == "file" && file.Name.EndsWith(".json")) {
                var fileUrl = file.DownloadUrl;

                response = await client.GetAsync(fileUrl);
                response.EnsureSuccessStatusCode();

                var fileContent = await response.Content.ReadAsStringAsync();
                this.LoadRaceConfig(fileContent);
            }
        }
    }

    private void LoadRaceConfig(string config) {
        this.RaceConfigs.Add(JsonSerializer.Deserialize<RaceConfigJson>(config, this.jsonOptions)!);
    }

    private record GitHubDirectoryContents {
        [JsonPropertyName("type")] public required string Type;
        [JsonPropertyName("name")] public required string Name;
        [JsonPropertyName("path")] public required string Path;
        [JsonPropertyName("download_url")] public required string DownloadUrl;
    }
}
