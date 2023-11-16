# Slop Crew FAQ

## How do I install Slop Crew?/How do I set up my own server? How do I compile the plugin from source?

Read the [install guide](https://github.com/SlopCrew/SlopCrew/blob/main/INSTALL.md). You'll find instructions for many ways to install or compile the plugin.

## Help! My game crashed!

Sorry about that! You can find crash details in `C:\Users\<your username>\AppData\Local\Temp\Team Reptile\Bomb Rush Cyberfunk\Crashes\`.

On Steam Deck, you can find crash details in `/home/deck/.local/share/Steam/steamapps/compatdata/1353230/pfx/drive_c/users/steamuser/Temp/Team Reptile/Bomb Rush Cyberfunk/Crashes/`.

If you can't troubleshoot it yourself, you can join our [Discord server](https://discord.gg/a2nVaZGGNz) and upload the contents of the latest folder into the `#troubleshooting` channel.

## Will you add mod syncing?

No. This would not only open up a near infinite amount of vulnerabilities, but also allow risque content to be shared through the mod, as well as put even more load on servers.

## Where is my BepInEx log?

- GOG/Steam Deck/Manual installs: Inside of the BepInEx folder in your game install.
- r2modman: See image.

![](https://xboxlive.party/i/gg8o8hxg.png)

## How do I change my name?

Edit the config file. Manual installs can find it in `<game location>/BepInEx/config`, and r2modman users can open the dedicated config editor on the left sidebar.

## How do I get colors in my name?

Unity rich text! Check [here](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/StyledText.html). Use it like `<color=#ff0000>NotNite`.

## What do the stars mean above people's names?

This means they're a community contributor. Community contributors have done something important for the project, usually helping with the development of the mod on GitHub.

![Screenshot of NotNite with the community contributor star above their name.](https://xboxlive.party/i/ojoen9un.png)

## My name is cut off!

To prevent protocol-level abuse from large names (and to partially inspire creativity), there is a 32 character limit on names, including Unity rich text tags. If you're hitting the limit from colors, try using [3 characters](<https://borderleft.com/toolbox/hex/>) instead of 6 for hex codes, or use [color names](<https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/StyledText.html#supported-colors>).

## My name is "Punished Slopper"!

That means your name tripped the profanity filter. The filter is not intended to block profanity as much as slurs, but the profanity filter used by Slop Crew is quite restrictive. An allowlist of names is implemented for people who trip the filter - please join our [Discord server](https://discord.gg/a2nVaZGGNz) and DM me (`@notnite`) with your config file if you'd like your name in the allowlist.

## Could you add `<feature>`? Is `<feature>` planned?

In order to not burn myself or our contributors out, and to not make more promises than I can deliver, I don't plan ahead on development. The only things that are planned are on [GitHub issues](https://github.com/SlopCrew/SlopCrew/issues).

I may look into certain features in the future, but it's not guaranteed. I go at my own pace.

## Will you add text chat?

No. Maybe quick chat with premade messages, but refer to the previous question.

## Where is my save file?

- Steam: `C:\Program Files (x86)\Steam\userdata\<your user id>\1353230\remote`
  - `<your user id>` will differ per person. This path is not changed if you installed the game on a different drive, but is changed if you installed Steam itself in a different location.
- GOG: `C:\Users\<your username>\AppData\LocalLow\Team Reptile\Bomb Rush Cyberfunk\SaveData`

If you wish to back your save files up, select `GameProgress<number>.brp` (along with the backup variants). The number is zero-indexed, so slot 1 is filename 0, etc.

## How do player battles work?

Open the Slop Crew app on your phone. Press up/down to cycle modes, and press right to send a battle request to the nearest player. The other player must either press the button at the same time or accept the notification on their phone to start the battle.

- Score Attack: The player with the most points in 90 seconds wins.
- Combo Attack: The player with the most points in one combo (or the player with the most points in 5 minutes) wins.

Your current score/multiplier will be reset when starting a battle. Please note some players may have plugins installed that change score mechanics, which may result in an unfair battle.

## How do I save a debug log?

Press **Ctrl**+**S**+**C**+**L** (**S**lop **C**rew **L**og) - it will open the File Explorer with the debug log selected. Upload this file in the `#⁠troubleshooting` channel in our [Discord server](https://discord.gg/a2nVaZGGNz) if you need help with something.
