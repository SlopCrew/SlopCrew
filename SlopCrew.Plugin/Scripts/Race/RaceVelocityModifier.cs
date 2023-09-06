using HarmonyLib;
using Reptile;
using UnityEngine;

namespace SlopCrew.Plugin.Scripts {
    public class RaceVelocityModifier : MonoBehaviour {
        //TODO: apply a stale move negation aka reduce amount of multiplier gain depending if the same trick/combination is used multiple times in a row
        //aka how many times the number of trick needed to affect speed

        private const int RACE_TRICK_MULTIPLIER = 5;
        private float originalBoostSpeedTarget = 0f;
        private float originalGrindSpeedMultiplier = 10;

        public static float BoostSpeedTarget { get; internal set; } = 0f;
        public static float GrindSpeedTarget { get; internal set; } = 0f;


        public void Awake() {
            DontDestroyOnLoad(gameObject);
        }

        public void Start() {
            var player = WorldHandler.instance.GetCurrentPlayer();
            originalBoostSpeedTarget = player.normalBoostSpeed;
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

            GrindSpeedTarget = originalGrindSpeedMultiplier + (tricksInCombo / RACE_TRICK_MULTIPLIER);
            BoostSpeedTarget = originalBoostSpeedTarget + (tricksInCombo / RACE_TRICK_MULTIPLIER);
        }
    }
}
