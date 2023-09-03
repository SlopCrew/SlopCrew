New-Item -ItemType Directory -Path ./local
Invoke-WebRequest -Uri https://sloppers.club/publicized.zip -OutFile ./local/BombRushCyberfunk.zip
Expand-Archive -Path ./local/BombRushCyberfunk.zip -DestinationPath ./local/BombRushCyberfunk
Remove-Item ./local/BombRushCyberfunk.zip
