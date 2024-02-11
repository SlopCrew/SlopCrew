# Slop Crew FAQ

## Help! My game crashed!

Sorry about that! You can find crash details in `C:\Users\<your username>\AppData\Local\Temp\Team Reptile\Bomb Rush Cyberfunk\Crashes`. On Steam Deck, you can find crash details in `/home/deck/.local/share/Steam/steamapps/compatdata/1353230/pfx/drive_c/users/steamuser/Temp/Team Reptile/Bomb Rush Cyberfunk/Crashes`.

If you can't troubleshoot it yourself, you can join our [Discord server](https://discord.gg/a2nVaZGGNz) and upload the contents of the latest folder into the `#troubleshooting` channel.

## How do I get colors in my name?

Unity rich text! Check [here](<https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/StyledText.html>). Use it like `<color=#ff0000>NotNite`. You can also omit part of it (`<#ff0000>NotNite`) or use 3 characters (`<#f00>NotNite`).

## How do I change my name?

Edit the config file. Manual installs can find it in `BepInEx/config`, and r2modman users can open the dedicated config editor on the left sidebar.

## What do the stars mean above people's names?

This means they're a community contributor. Community contributors have done something important for the project, usually helping with the development of the mod on GitHub.

**Anyone asking for how to get the badge, or making contributions for the sole purpose of getting the badge, will be laughed at.**

![Screenshot of NotNite with the community contributor star above their name.](https://xboxlive.party/i/ojoen9un.png)

## Could you add \<x>?/Is \<x> planned?

In order to not burn myself out, and to not make more promises than I can deliver, I don't plan ahead on development. The only things that are planned are on [GitHub issues](<https://github.com/SlopCrew/SlopCrew/issues>).

I may look into certain features in the future, but it's not guaranteed. I go at my own pace.

## My name is cut off!

To prevent protocol-level abuse from large names (and to partially inspire creativity), there is a 32 character limit on names, including Unity rich text tags. If you're hitting the limit from colors, try using [3 characters](<https://borderleft.com/toolbox/hex/>) instead of 6 for hex codes, or use [color names](<https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/StyledText.html#supported-colors>).

## My name is "Punished Slopper"!

That means your name tripped the profanity filter. The filter is not intended to block profanity as much as slurs, but the profanity filter used by Slop Crew is quite restrictive. An allowlist of names is implemented for people who trip the filter - please DM me with your config file if you'd like your name in the allowlist.

You can thank the man who named themselves the N-word a few days ago for this filter being implemented.

## Where is my BepInEx log?

- r2modman: See attached image.
- GOG/Steam Deck/Manual installs: Inside of the BepInEx folder in your game install.

![](https://xboxlive.party/i/gg8o8hxg.png)

## Where is my save file?

- Steam: `C:\Program Files (x86)\Steam\userdata\<your user id>\1353230\remote`
  - `<your user id>` will differ per person. This path is not changed if you installed the game on a different drive, but is changed if you installed Steam itself in a different location.
- GOG: `C:\Users\<your username>\AppData\LocalLow\Team Reptile\Bomb Rush Cyberfunk\SaveData`

If you wish to back your save files up, select `GameProgress<number>.brp` (along with the backup variants). The number is zero-indexed, so slot 1 is filename 0, etc.

## How do activities work?

Open the Slop Crew app on your phone and select a mode. You will not be allowed to select a mode if you have mods installed that provide an unfair advantage - disabling them and restarting the game will allow you to.

### Score/Combo battles

The other player must either press the button at the same time or accept the notification on their phone to start the battle.

In score battles, the player with the most points at the end of the timer wins. In combo battles, the player with the most points in one combo (or at the end of the timer) wins.

Your current score/multiplier will be reset when starting a battle.

### Race

Select the race mode at the same time as other players. This internally creates a lobby, which will start after enough time has passed. You can only queue with players in the same stage as you.

## I can't connect to Slop Crew!

This usually means you are either running an out of date version of the Slop Crew plugin or you are having networking issues. Please provide some more information in ⁠the troubleshooting channel in our [Discord server.](https://discord.gg/a2nVaZGGNz)

- Provide a BepInEx log: ⁠⁠Read "Where is my BepInEx log?" in this FAQ for help locating this file.
- Try and connect to the [web server](https://sloppers.club/test.txt) and see if it works. The expected result is a small smiley face ( :) ).
- Include your Internet service provider and country if you are comfortable doing so. Do not post your IP address unless explicitly requested by an admin (@biggest slopper role in our [Discord server.](https://discord.gg/a2nVaZGGNz)) to provide it in DMs.
- If you are using a VPN, commercial antivirus/firewall, or corporate/educational network, please state the relevant product/service.

Keep in mind that most Slop Crew connection issues are related to a specific issue with your network, so it's hard to solve a lot of these problems.
