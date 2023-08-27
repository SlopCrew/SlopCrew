# Install Guide

## Steam

- Install [r2modman](https://thunderstore.io/c/bomb-rush-cyberfunk/p/ebkr/r2modman/).
- Start r2modman and select `Bomb Rush Cyberfunk` from the game list.
  - If it doesn't appear, update r2modman (either in the settings or by rerunning the installer).
- Go to the `Online` tab on the left and download Slop Crew.
  - If it opens a window prompting to install dependencies, click `Install with Dependencies`.
- Start the game with the `Start Modded` button in the top left, and close the game again. This will generate your config file.
- Optional, but suggested: Click the `Config Editor` tab on the left side and select the Slop Crew configuration file to change settings (like your name).

## GOG/Manual installs

- Download [BepInEx 5](https://github.com/BepInEx/BepInEx/releases/download/v5.4.21/BepInEx_x64_5.4.21.0.zip).
- Drop the zip file into your game folder. **Extract its contents**, do ***not*** extract it into a new folder. You should now have a file called `winhttp.dll` and a `BepInEx` folder next to `Bomb Rush Cyberfunk.exe`. You can now delete the zip file.
- Start the game and close it. This will generate additional BepInEx directories.
- Download Slop Crew [from GitHub](https://github.com/NotNite/SlopCrew/releases).
- Navigate to your `Bomb Rush Cyberfunk\BepInEx\plugins` directory. Extract Slop Crew in there. As long as the SlopCrew DLL files are *somewhere* in that `BepInEx\plugins` directory, the mod will load.
- Start the game, and close it once more. This will generate the config file.
- Optional, but suggested: navigate to your `Bomb Rush Cyberfunk\BepInEx\config` directory and open `SlopCrew.Plugin.cfg` with any text editor to change settings (like your name).

## Steam Deck/Linux

Follow the same steps as [GOG/manual installs](#gogmanual-installs), but before starting the game for the first time, set the `WINEDLLOVERRIDES="winhttp=n,b"` environment variable. This will allow BepInEx to load.

Users launching from Steam can insert `WINEDLLOVERRIDES="winhttp=n,b" %command%` into the Steam launch options.

## Custom servers

Follow the instructions for your operating system below. Afterwards, you will need to enable accessing your server, through one of many means:

- (Suggested for newcomers) Use a VPN like Tailscale, Radmin, or ZeroTier to create a private network between your friends.
- Port forward the server through your router and share your public IP with your friends.
- Run the server through a reverse proxy, like NGINX or Caddy (making sure to setup WebSocket support).

The server listens on all interfaces on port 42069 by default - this can be changed with the `SLOP_INTERFACE` environment variable.

### Windows

- Download the server binaries [from GitHub Actions](https://github.com/NotNite/SlopCrew/actions/workflows/server-build.yml?query=branch%3Amain+event%3Apush).
  - Select the first entry, scroll down, and select `server-windows` from the Artifacts section.
  - You will need a GitHub account to download these artifacts.
- Download the [.NET 7 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-7.0.10-windows-x64-installer).
- Start the executable by double clicking it.

### Linux

Build the repository from source:

```shell
$ git clone https://github.com/NotNite/SlopCrew.git
$ cd SlopCrew
$ dotnet run SlopCrew.Server --configuration Release
```

Docker users can also use the `Dockerfile`/`docker-compose.yml`, or make their own using the image at `ghcr.io/notnite/slopcrew-server:latest`.

## Compiling the plugin (for developers)

The `SlopCrew.Plugin` project references DLLs in your game install. To not commit piracy, the location to your game file must be specified with the `BRCPath` variable.

This path will vary per person, and will point to the folder that contains the game executable *without a trailing slash* (e.g. `F:\games\steam\steamapps\common\BombRushCyberfunk`).

- Visual Studio: Set `BRCPath` as a global environment variable (I haven't figured out how to set it per-project yet).
- JetBrains Rider: Go to `File | Settings | Build, Execution, Deployment | Toolset and Build` and edit the MSBuild global properties.
- dotnet CLI: Pass `-p:BRCPath="path/to/game"` as an argument.
