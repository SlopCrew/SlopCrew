#!/usr/bin/env bash
mkdir -p ./thunderstore/BepInEx/plugins/SlopCrew.Plugin
rm -rf ./thunderstore/BepInEx/plugins/SlopCrew.Plugin/*
cp -r ./SlopCrew.Plugin/bin/Debug/net462/* ./thunderstore/BepInEx/plugins/SlopCrew.Plugin
cp ./README.md ./thunderstore
