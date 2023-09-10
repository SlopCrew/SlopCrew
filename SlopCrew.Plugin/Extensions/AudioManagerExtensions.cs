using HarmonyLib;
using Reptile;

namespace SlopCrew.Plugin.Extensions; 

public static class AudioManagerExtensions {
    public static void PlaySfx(
        this AudioManager audioManager, SfxCollectionID sfxCollectionID, AudioClipID audioClipID, float pitch = 0f
    ) {
        Traverse.Create(audioManager).Method("PlaySfxUI", sfxCollectionID, audioClipID, pitch).GetValue();
    }

    public static void PlayVoice(this AudioManager audioManager, Characters character, AudioClipID audioClipID) {
        Traverse.Create(audioManager).Method("PlayVoice", character, audioClipID).GetValue();
    }
}
