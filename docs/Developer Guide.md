# Developer Guide

## GameNetworkingSockets

Slop Crew uses [GameNetworkingSockets](https://github.com/ValveSoftware/GameNetworkingSockets) for networking. This can be built on Windows with [vcpkg](https://vcpkg.io/):

```shell
vcpkg install
```

Copy the resulting binaries (`.dll` or `.so`) into `libs/GameNetworkingSockets`. You can also copy the binaries from the CI artifacts if you're lazy.

## Building Slop Crew

The `SlopCrew.Plugin` project references DLLs in your game install. To not commit piracy, the location to your game file must be specified with the `BRCPath` variable.

This path will vary per person, and will point to the folder that contains the game executable *without a trailing slash* (e.g. `F:\games\steam\steamapps\common\BombRushCyberfunk`).

- Visual Studio: Create `SlopCrew.Plugin.csproj.user` next to the original `.csproj`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project>
  <PropertyGroup>
    <BRCPath>path/to/game</BRCPath>
    <ManagedPath>$(BRCPath)/Bomb Rush Cyberfunk_Data/Managed</ManagedPath>
  </PropertyGroup>
</Project>
```

- JetBrains Rider: Go to `File | Settings | Build, Execution, Deployment | Toolset and Build` and edit the MSBuild global properties.
- dotnet CLI: Pass `-p:BRCPath="path/to/game"` as an argument.

Linux users will need to acquire the Windows GameNetworkingSockets binaries, along with setting the `SLOPCREW_FORCE_WINDOWS` environment variable to true, when building the plugin.

## Using the API

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
