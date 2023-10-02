using Reptile;
using SlopCrew.Common;
using SlopCrew.Plugin.ModCompatability;
using System;

namespace SlopCrew.Plugin;

public class CharacterInfoManager {
    public CustomCharacterInfo GetCharacterInfo(int character) {
        if (CrewBoomAPI.CrewBoomAPIDatabase.IsInitialized &&
            CrewBoomAPI.CrewBoomAPIDatabase.GetUserGuidForCharacter(character, out var guid)) {
            //Plugin.Log.LogInfo($"Using CrewBoom, GUID {guid}");

            return new CustomCharacterInfo {
                Method = CustomCharacterInfo.CustomCharacterMethod.CrewBoom,
                Data = guid.ToString()
            };
        }

        if(CharacterAPIModCompat.enabled) {
            var hash = CharacterAPIModCompat.GetModdedCharacterHash((Characters)character);
            if(hash != 0) {
                return new CustomCharacterInfo {
                    Method = CustomCharacterInfo.CustomCharacterMethod.CharacterAPI,
                    Data = hash.ToString()
                };
            }
        }

        return new CustomCharacterInfo {
            Method = CustomCharacterInfo.CustomCharacterMethod.None,
            Data = string.Empty
        };
    }

    public void SetNextCharacterInfo(CustomCharacterInfo info) {
        switch (info.Method) {
            case CustomCharacterInfo.CustomCharacterMethod.CrewBoom: {
                var guid = Guid.Parse(info.Data);
                //Plugin.Log.LogInfo($"Overriding with CrewBoom, GUID {guid}");
                CrewBoomAPI.CrewBoomAPIDatabase.OverrideNextCharacterLoadedWithGuid(guid);
                break;
            }
        }
    }
}
