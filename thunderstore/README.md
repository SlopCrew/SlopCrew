# Slop Crew

"multiplayer mod for [skateboard video game](https://store.steampowered.com/app/1353230/Bomb_Rush_Cyberfunk/)"

![Several players in a Slop Crew server](https://namazu.photos/i/a7rb2n7s.png)

---

It's a Bomb Rush Cyberfunk multiplayer mod. Skate around with your friends.

**WARNING:** This is a highly unstable mod with several bugs and crashes. I'm working on it as I can, but please don't expect things to be *too* stable.

## Supported features

Slop Crew can sync:

- Character, movestyle, and outfits
- Player position, including across maps
- Boostpack/friction effects
- Animations & dancing

## Connecting to a server

After install, Slop Crew will make a config file you can edit. The default server is hosted by me ([NotNet](https://n2.pm/)) in the eastern US.

After starting the game, open a save file, and other players will automagically appear in front of you. Have fun!

## Running your own server

Binaries are available [on GitHub Actions](https://github.com/NotNite/SlopCrew/actions), which require a GitHub account to download. You can also build from source.

Start the executable, optionally setting the `SLOP_INTERFACE` environment variable to change the port (by default `ws://0.0.0.0:42069`), and you're good to go. Port forwarding or reverse proxying is required.
