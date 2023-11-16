using System;
using System.Collections.Generic;
using Google.Protobuf;
using SlopCrew.Common.Proto;

namespace SlopCrew.Plugin;

public class CharacterInfoManager {
    public List<CustomCharacterInfo> GetCharacterInfo(int character) {
        var infos = new List<CustomCharacterInfo>();
        
        if (CrewBoomAPI.CrewBoomAPIDatabase.IsInitialized &&
            CrewBoomAPI.CrewBoomAPIDatabase.GetUserGuidForCharacter(character, out var guid)) {
            infos.Add(new CustomCharacterInfo {
                Type = CustomCharacterInfoType.CrewBoom,
                Data = ByteString.CopyFrom(guid.ToByteArray())
            });
        }

        return infos;
    }


    public void ProcessCharacterInfo(List<CustomCharacterInfo> infos) {
        foreach (var info in infos) {
            switch (info.Type) {
                case CustomCharacterInfoType.CrewBoom: {
                    var guid = new Guid(info.Data.ToByteArray());
                    CrewBoomAPI.CrewBoomAPIDatabase.OverrideNextCharacterLoadedWithGuid(guid);
                    break;
                }
            }
        }
    }
}
