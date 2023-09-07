using HarmonyLib;
using Reptile;
using Reptile.Phone;
using SlopCrew.Plugin.UI.Phone;

namespace SlopCrew.Plugin.Patches {
    [HarmonyPatch(typeof(WorldHandler))]
    internal class WorldHandlerPatchs {

        [HarmonyPrefix]
        [HarmonyPatch("UpdateWorldHandler")]
        public static void UpdateWorldHandlerPrefix() {
            BaseModule instance = Traverse.Create(typeof(BaseModule)).Field("instance").GetValue<BaseModule>();
            Stage currentStage = Traverse.Create(instance).Field("currentStage").GetValue<Stage>();

            if (Plugin.RaceManager.HasAdditionRaceConfigToLoad() && currentStage == Plugin.RaceManager.GetStage()) {
                Plugin.RaceManager.AdditionalRaceInitialization();
                Plugin.RaceManager.SetHasAdditionRaceConfigToLoad(false);
                var player = WorldHandler.instance.GetCurrentPlayer();
                Phone phone = Traverse.Create(player).Field<Phone>("phone").Value;
                phone.OpenApp(typeof(AppSlopCrew));
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("UpdateWorldHandler")]
        public static void UpdateWorldHandlerPostfix() {
            if (Plugin.RaceManager.ShouldLoadRaceStageASAP()) {
                BaseModule instance = Traverse.Create(typeof(BaseModule)).Field("instance").GetValue<BaseModule>();
                Stage toLoad = Plugin.RaceManager.GetStage();

                Plugin.RaceManager.SetShouldLoadRaceStageASAP(false);
                Plugin.RaceManager.SetHasAdditionRaceConfigToLoad(true);
                instance.SwitchStage(toLoad);
            }
        }
    }
}
