$ErrorActionPreference = "Stop"

tcli publish `
--file ./out/thunderstore.zip `
--config-path ./thunderstore/thunderstore.toml `
--token $env:THUNDERSTORE_TOKEN `
--repository "https://thunderstore.io"
