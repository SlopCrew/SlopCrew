# Server Guide

## Getting the server binaries

### Windows

- Download the server binaries [from GitHub Actions](https://github.com/SlopCrew/SlopCrew/actions/workflows/server.yml?query=branch%3Amain+event%3Apush) (pick the latest one).
  - After selecting the entry, scroll down to the bottom, and select `server-windows` from the Artifacts section. You will need a GitHub account to download these artifacts, or you can use a service like [nightly.link](https://nightly.link) if you do not have one.
- Download the [.NET 7 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-7.0.10-windows-x64-installer).
- Start the executable by double clicking it.

### Linux

Either [build the server](https://github.com/SlopCrew/SlopCrew/blob/main/docs/Developer%20Guide.md) from source, or use the Docker container:

- Building from source needs .NET 8, along with native GameNetworkingSockets binaries that can be built through vcpkg.
- The Dockerfile in this repository is available from ghcr.io as `ghcr.io/slopcrew/slopcrew-server`.

## Connecting to the server

You will need to enable accessing your server in one of a few ways:

- (Suggested for newcomers) Use a VPN like Tailscale, Radmin, or ZeroTier to create a private network between your friends.
- Port forward the server through your router and share your public IP with your friends.
- (Advanced users only) Run the Slop Crew server remotely on a server host (e.g. a cloud VPS).

The server exposes two ports:

- an HTTP web server (default port seemingly variable, TCP)
- the Slop Crew game server (default port 42069, UDP)

Both ports can be customized, so now would be a good time to pick what ports you want to use. It is suggested to use a reverse proxy for the web server.

## Configuring the server

The server can be configured like a regular ASP.NET Core app - through an `appsettings.json` or environment variables (with the format `Category__Key=Value`). Here are the default settings:

```json
{
  "Auth": {
    "JwtSecret": "",
    "DiscordClientId": "",
    "DiscordClientSecret": "",
    "DiscordRedirectUri": "",
    "AdminSecret": ""
  },
  "Database": {
    "DatabasePath": "database.db"
  },
  "Encounter": {
    "BannedPlugins": [],
    "ScoreBattleLength": 180,
    "ComboBattleLength": 300,
    "ComboBattleGrace": 15,
    "RaceConfigDirectory": null
  },
  "Graphite": {
    "Host": null,
    "Port": 2003
  },
  "Server": {
    "Port": 42069,
    "TickRate": 10,
    "QuieterLogs": false
  }
}
```

To change the web server port, it is suggested to use the `ASPNETCORE_URLS` environment variable (example value `http://*:8080`).

The Discord-related keys in the Auth category are only used for [SlopNet](https://github.com/SlopCrew/slopnet), and are not required to start the server. The `AdminSecret` is only required to use the admin API.

Docker users will want to mount their `appsettings.json` to `/app/appsettings.json`, and set the database path to a mounted volume for persistent storage.
