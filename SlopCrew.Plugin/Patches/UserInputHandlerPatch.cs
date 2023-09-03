using HarmonyLib;
using Reptile;
using static Reptile.UserInputHandler;

namespace SlopCrew.Plugin.Patches {
    [HarmonyPatch(typeof(UserInputHandler))]
    public class UserInputHandlerPatch {

        [HarmonyPrefix]
        [HarmonyPatch("PollInputs")]
        public static bool PollInputsPrefix(ref InputBuffer __result, ref InputBuffer inputBuffer) {
            if (Plugin.ShouldIgnoreInput) {
                __result = new InputBuffer();
                inputBuffer = new InputBuffer();
                return false;
            }

            return true;
        }
    }
}
