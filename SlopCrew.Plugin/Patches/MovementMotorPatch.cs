using HarmonyLib;
using Reptile;
using SlopCrew.Plugin.Encounters;

namespace SlopCrew.Plugin.Patches {
    [HarmonyPatch(typeof(MovementMotor))]
    public class MovementMotorPatch {
        /// <summary>
        /// Ignore modifying collision on grind in race
        /// <para>This is to prevent checkpoint not being triggered when grinding</para>
        /// <para>This might have repercussion on gameplay (nothing happened when i tested ¯\_(ツ)_/¯)</para>
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch("HaveCollision")]
        public static bool HaveCollision(bool have) {
            var worldHandler = WorldHandler.instance;
            var currentPlayer = worldHandler.GetCurrentPlayer();

            return Plugin.CurrentEncounter is not SlopRaceEncounter {IsBusy: true} || !currentPlayer.CanStartGrind();
        }
    }
}
