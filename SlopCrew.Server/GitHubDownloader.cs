using Serilog;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SlopCrew.Server {
    internal class GitHubDownloader {
        public async Task<Dictionary<string, string>> DownloadFilesFromDirectory(string repositoryOwner, string repositoryName, string directoryPath = "") {
            var files = new Dictionary<string, string>();

            using var client = new HttpClient();

            var apiUrl = $"https://api.github.com/repos/{repositoryOwner}/{repositoryName}/contents/{directoryPath}?ref=main";
            client.DefaultRequestHeaders.Add("User-Agent", "C# HttpClient");
            var response = await client.GetAsync(apiUrl);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var contents = JsonSerializer.Deserialize<List<GitHubDirectoryContents>>(content);

            if (contents == null) {
                Log.Information($"No file found at {apiUrl}");
                return files;
            }

            foreach (var file in contents) {
                if (file.Type == "file" && file.Name.EndsWith(".json")) {
                    var fileUrl = file.DownloadUrl;

                    response = await client.GetAsync(fileUrl);
                    response.EnsureSuccessStatusCode();

                    var fileContent = await response.Content.ReadAsStringAsync();
                    files.Add(file.Name, fileContent);
                }
            }

            return files;
        }

        class GitHubDirectoryContents {
            [JsonPropertyName("type")]
            public string Type { get; set; }

            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("path")]
            public string Path { get; set; }

            [JsonPropertyName("download_url")]
            public string DownloadUrl { get; set; }
        }
    }
}
