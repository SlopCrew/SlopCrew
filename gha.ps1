# https://github.com/BepInEx/BepInEx.AssemblyPublicizer
# assembly-publicizer "./local/BombRushCyberfunk/Bomb Rush Cyberfunk_Data/Managed/" --overwrite --strip-only
New-Item -ItemType Directory -Path ./local
Invoke-WebRequest -Uri https://sloppers.club/publicized.zip -OutFile ./local/BombRushCyberfunk.zip
Expand-Archive -Path ./local/BombRushCyberfunk.zip -DestinationPath ./local/BombRushCyberfunk
Remove-Item ./local/BombRushCyberfunk.zip
