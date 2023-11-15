using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using Reptile;
using SlopCrew.Plugin.Encounters;
using RaceEncounter = SlopCrew.Plugin.Encounters.RaceEncounter;

namespace SlopCrew.Plugin.Patches;

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
        var encounterManager = Plugin.Host.Services.GetRequiredService<EncounterManager>();
        return encounterManager.CurrentEncounter is not RaceEncounter {IsBusy: true} || !currentPlayer.CanStartGrind();
    }
}
