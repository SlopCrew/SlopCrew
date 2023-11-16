using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using Reptile;
using static Reptile.UserInputHandler;

namespace SlopCrew.Plugin.Patches;

[HarmonyPatch(typeof(UserInputHandler))]
public class UserInputHandlerPatch {
    [HarmonyPrefix]
    [HarmonyPatch("PollInputs")]
    public static bool PollInputsPrefix(ref InputBuffer __result, ref InputBuffer inputBuffer) {
        var inputBlocker = Plugin.Host.Services.GetRequiredService<InputBlocker>();
        if (inputBlocker.ShouldIgnoreInput) {
            __result = new InputBuffer();
            inputBuffer = new InputBuffer();
            return false;
        }

        return true;
    }
}
