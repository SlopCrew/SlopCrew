using HarmonyLib;
using Reptile;
using SlopCrew.Plugin.Scripts;

namespace SlopCrew.Plugin.Patches {
    [HarmonyPatch(typeof(WorldHandler))]
    internal class WorldHandlerPatchs {

        [HarmonyPrefix]
        [HarmonyPatch("UpdateWorldHandler")]
        public static void UpdateWorldHandlerPrefix() {

            BaseModule instance = Traverse.Create(typeof(BaseModule)).Field("instance").GetValue<BaseModule>();
            Stage currentStage = Traverse.Create(instance).Field("currentStage").GetValue<Stage>();

            if (RaceManager.Instance.HasAdditionRaceConfigToLoad() && currentStage == RaceManager.Instance.GetStage()) {
                RaceManager.Instance.AdditionalRaceInitialization();
                RaceManager.Instance.SetHasAdditionRaceConfigToLoad(false);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("UpdateWorldHandler")]
        public static void UpdateWorldHandlerPostfix() {
            if (RaceManager.Instance.ShouldLoadRaceStageASAP()) {
                BaseModule instance = Traverse.Create(typeof(BaseModule)).Field("instance").GetValue<BaseModule>();
                Stage toLoad = RaceManager.Instance.GetStage();

                RaceManager.Instance.SetShouldLoadRaceStageASAP(false);
                RaceManager.Instance.SetHasAdditionRaceConfigToLoad(true);
                instance.SwitchStage(toLoad);
            }

        }
    }
}
