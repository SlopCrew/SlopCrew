using System;
using SlopCrew.Common;

namespace SlopCrew.Plugin;

public class CharacterInfoManager {
    public CustomCharacterInfo GetCharacterInfo(int character) {
        if (BrcCustomCharactersAPI.Database.IsInitialized &&
            BrcCustomCharactersAPI.Database.GetUserGuidForCharacter(character, out var guid)) {
            Plugin.Log.LogInfo($"Using BrcCustomCharacters, GUID {guid}");

            return new CustomCharacterInfo {
                Method = CustomCharacterInfo.CustomCharacterMethod.BrcCustomCharacters,
                Data = guid.ToString()
            };
        }

        return new CustomCharacterInfo {
            Method = CustomCharacterInfo.CustomCharacterMethod.None,
            Data = string.Empty
        };
    }

    public void SetNextCharacterInfo(CustomCharacterInfo info) {
        switch (info.Method) {
            case CustomCharacterInfo.CustomCharacterMethod.BrcCustomCharacters: {
                var guid = Guid.Parse(info.Data);
                Plugin.Log.LogInfo($"Overriding with BrcCustomCharacters, GUID {guid}");
                BrcCustomCharactersAPI.Database.OverrideNextCharacterLoadedWithGuid(guid);
                break;
            }
        }
    }
}
