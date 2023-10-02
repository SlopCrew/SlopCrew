using Reptile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlopCrew.Plugin.ModCompatability {
    public static class CharacterAPIModCompat {

        private static bool? _enabled;

        public static bool enabled {
            get {
                if (_enabled == null) {
                    _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.Viliger.CharacterAPI");
                }
                return (bool) _enabled;
            }
        }

        public static bool ModdedCharacterExists(int hash, out Characters character) {

            var moddedCharacter = CharacterAPI.ModdedCharacter.GetModdedCharacter(hash);
            if (moddedCharacter != null) {
                character = moddedCharacter.characterEnum;
                return true;
            }

            character = Characters.metalHead;
            return false;
        }

        public static int GetModdedCharacterHash(Characters character) {
            var moddedCharacter = CharacterAPI.ModdedCharacter.GetModdedCharacter(character);
            if (moddedCharacter == null) {
                return 0;
            }

            return moddedCharacter.GetHashCode();
        }

    }
}
