# Slop Crew

multiplayer mod for [skateboard video game](https://store.steampowered.com/app/1353230/Bomb_Rush_Cyberfunk/)

## USAGE

### Manual install

- install BepInEx 5, either from [GitHub](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.21) or [Thunderstore/r2modman](https://thunderstore.io/c/bomb-rush-cyberfunk/p/BepInEx/BepInExPack/)
- ~~download latest release~~ (soon) or Compile It Yourself
- extract plugin into `steamapps\common\BombRushCyberfunk\BepInEx\plugins` (you should now have a `SlopCrew.Plugin` folder in there with a bunch of DLLs)
- start the game, load into the menu, then quit
- go to `steamapps\common\BombRushCyberfunk\BepInEx\config` and open `SlopCrew.Plugin.cfg` in a text editor
- set your name (max 32 characters) and set the server address to connect to
- save, boot the game back up, load your save and done! if the server you joined is up and running, you should see people sloppin around.

### r2modman

- tbd

## BUILDING

- set the `BRCPath` MSBuild global property
  - mine is `F:\games\steam\steamapps\common\BombRushCyberfunk`
- build it
- ez
