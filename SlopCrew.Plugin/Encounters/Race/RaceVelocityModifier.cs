using HarmonyLib;
using Reptile;
using UnityEngine;

namespace SlopCrew.Plugin.Encounters.Race;

// TODO make this not a monobehavior
public class RaceVelocityModifier : MonoBehaviour {
    private const float RaceTrickMultiplier = 5f;
    public static float BoostSpeedTarget;
    public static float GrindSpeedTarget;

    private static float OriginalBoostSpeedTarget;
    private const float OriginalGrindSpeedMultiplier = 10;

    public void Awake() {
        DontDestroyOnLoad(gameObject);
    }

    public void Start() {
        var player = WorldHandler.instance.GetCurrentPlayer();
        OriginalBoostSpeedTarget = player.normalBoostSpeed;
    }

    public void Update() {
        if (WorldHandler.instance != null) {
            var player = WorldHandler.instance.GetCurrentPlayer();
            if (player != null) {
                UpdateTargetSpeed();
            }
        }
    }

    private void UpdateTargetSpeed() {
        var player = WorldHandler.instance.GetCurrentPlayer();
        var tricksInCombo = Traverse.Create(player).Field<int>("tricksInCombo").Value;

        GrindSpeedTarget = OriginalGrindSpeedMultiplier + (tricksInCombo / RaceTrickMultiplier);
        BoostSpeedTarget = OriginalBoostSpeedTarget + (tricksInCombo / RaceTrickMultiplier);
    }
}
