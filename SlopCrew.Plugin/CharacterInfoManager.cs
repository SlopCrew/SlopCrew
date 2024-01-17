using System;
using System.Collections.Generic;
using Google.Protobuf;
using SlopCrew.API;
using SlopCrew.Common.Proto;

namespace SlopCrew.Plugin;

public class CharacterInfoManager : IDisposable {
    private readonly SlopCrewAPI api;
    public readonly Dictionary<string, byte[]> CustomCharacterInfo = new();

    public CharacterInfoManager(SlopCrewAPI api) {
        this.api = api;
        this.api.OnCustomCharacterInfoSet += this.OnCustomCharacterInfoSet;
    }

    public void Dispose() {
        this.api.OnCustomCharacterInfoSet -= this.OnCustomCharacterInfoSet;
    }

    private void OnCustomCharacterInfoSet(string id, byte[]? data) {
        if (data == null) {
            this.CustomCharacterInfo.Remove(id);
        } else {
            this.CustomCharacterInfo[id] = data;
        }
    }

    public List<CustomCharacterInfo> GetCharacterInfo(int character) {
        var infos = new List<CustomCharacterInfo>();

        if (CrewBoomAPI.CrewBoomAPIDatabase.IsInitialized &&
            CrewBoomAPI.CrewBoomAPIDatabase.GetUserGuidForCharacter(character, out var guid)) {
            infos.Add(new CustomCharacterInfo {
                Id = "CrewBoom",
                Data = ByteString.CopyFrom(guid.ToByteArray())
            });
        }

        foreach (var kvp in this.CustomCharacterInfo) {
            infos.Add(new CustomCharacterInfo {
                Id = kvp.Key,
                Data = ByteString.CopyFrom(kvp.Value)
            });
        }

        return infos;
    }


    public void ProcessCharacterInfo(List<CustomCharacterInfo> infos) {
        foreach (var info in infos) {
            switch (info.Id) {
                case "CrewBoom": {
                    var guid = new Guid(info.Data.ToByteArray());
                    CrewBoomAPI.CrewBoomAPIDatabase.OverrideNextCharacterLoadedWithGuid(guid);
                    break;
                }
            }
        }
    }
}
