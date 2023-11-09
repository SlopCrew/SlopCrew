# Install Guide

## Steam

- Install [r2modman](https://thunderstore.io/c/bomb-rush-cyberfunk/p/ebkr/r2modman/).
- Start r2modman and select `Bomb Rush Cyberfunk` from the game list.
  - If it doesn't appear, update r2modman (either in the settings or by rerunning the installer).
- Go to the `Online` tab on the left and download Slop Crew.
  - If it opens a window prompting to install dependencies, click `Install with Dependencies`.
- Start the game with the `Start Modded` button in the top left, and close the game again. This will generate your config file.
- Optional, but suggested: Click the `Config Editor` tab on the left side and select the Slop Crew configuration file to change settings (like your name).

To update Slop Crew, open r2modman, select Slop Crew in the Installed tab, and click Update.

## GOG/Manual installs

- Download [BepInEx 5](https://github.com/BepInEx/BepInEx/releases/download/v5.4.21/BepInEx_x64_5.4.21.0.zip).
- Drop the zip file into your game folder. **Extract its contents**, do ***not*** extract it into a new folder. You should now have a file called `winhttp.dll` and a `BepInEx` folder next to `Bomb Rush Cyberfunk.exe`. You can now delete the zip file.
- Start the game and close it. This will generate additional BepInEx directories.
- Download Slop Crew [from GitHub](https://github.com/SlopCrew/SlopCrew/releases).
- Navigate to your `Bomb Rush Cyberfunk\BepInEx\plugins` directory. Extract Slop Crew in there. As long as the Slop Crew DLL files are *somewhere* in that `BepInEx\plugins` directory, the mod will load.
  - While it is not required, for ease of updating, it is suggested to create a folder for the plugin files.
- Start the game, and close it once more. This will generate the config file.
- Optional, but suggested: navigate to your `Bomb Rush Cyberfunk\BepInEx\config` directory and open `SlopCrew.Plugin.cfg` with any text editor to change settings (like your name).

To update Slop Crew, delete all existing Slop Crew files in the `BepInEx\plugins` folder, and download & extract the new version from GitHub.

## Steam Deck/Linux

Follow the same steps as [GOG/manual installs](#gogmanual-installs), but before starting the game for the first time, set the `WINEDLLOVERRIDES="winhttp=n,b"` environment variable. This will allow BepInEx to load.

Users launching from Steam can insert `WINEDLLOVERRIDES="winhttp=n,b" %command%` into the Steam launch options.

## Custom servers

TODO

## Stuff for developers

### GameNetworkingSockets

Slop Crew uses [GameNetworkingSockets](https://github.com/ValveSoftware/GameNetworkingSockets) for networking. This can be built on Windows with [vcpkg](https://vcpkg.io/):

```shell
vcpkg install
```

### Building Slop Crew

The `SlopCrew.Plugin` project references DLLs in your game install. To not commit piracy, the location to your game file must be specified with the `BRCPath` variable.

This path will vary per person, and will point to the folder that contains the game executable *without a trailing slash* (e.g. `F:\games\steam\steamapps\common\BombRushCyberfunk`).

- Visual Studio: Set `BRCPath` as a global environment variable (I haven't figured out how to set it per-project yet).
- JetBrains Rider: Go to `File | Settings | Build, Execution, Deployment | Toolset and Build` and edit the MSBuild global properties.
- dotnet CLI: Pass `-p:BRCPath="path/to/game"` as an argument.

### Using the API

Slop Crew features an API you can use in your own BepInEx plugin. First, submodule this repository in your own code:

```shell
git submodule add https://github.com/SlopCrew/SlopCrew.git SlopCrew
```

Next, add the `SlopCrew.API` project as a reference to your project (adding it to your solution beforehand).

Now, you can use the API in your code. Here's a short example:

```cs
using SlopCrew.API;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInProcess("Bomb Rush Cyberfunk.exe")]
public class Plugin : BaseUnityPlugin {
    private void Awake() {
        // Access the API directly - this may be null
        // (e.g. Slop Crew isn't installed or hasn't loaded yet)
        var api = APIManager.API;
        this.Logger.LogInfo("Player count: " + api?.PlayerCount);

        // You can also use the event for when Slop Crew is loaded
        // Note that this will not fire if Slop Crew is loaded before yours; check for
        // the API being null before registering the event
        APIManager.OnAPIRegistered += (api) => {
            this.Logger.LogInfo("Player count: " + api.PlayerCount);
        };
    }
}
```

The API allows you to access information about Slop Crew (player count, server address, connection status) and listen for when it changes via events (player count changes, connects/disconnects).

It's intended that your plugin builds and ships with the SlopCrew.API assembly - do not remove it. Slop Crew also ships with the assembly, and will populate the API field when it loads. The API does not contain any Slop Crew functionality, and your plugin does not need to mark Slop Crew as a dependency on Thunderstore.
