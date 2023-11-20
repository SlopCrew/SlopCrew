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
