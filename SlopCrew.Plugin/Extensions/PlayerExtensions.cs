using Reptile;
using UnityEngine;

namespace SlopCrew.Plugin.Extensions;

public static class PlayerExtensions {
    public static int PlayRandomDance(this Player player) {
        var danceIndex = Random.Range(0, 6);
        var animID = Animator.StringToHash(((Dances) danceIndex).ToString().ToLower());
        player.PlayAnim(animID);
        return animID;
    }
}
