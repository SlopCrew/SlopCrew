using DG.Tweening;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using Reptile;
using SlopCrew.Common;
using SlopCrew.Plugin.UI;
using TMPro;
using UnityEngine;

namespace SlopCrew.Plugin.Patches;

[HarmonyPatch(typeof(VersionUIHandler))]
public class VersionUIHandlerPatch {
    [HarmonyPostfix]
    [HarmonyPatch("SetVersionText")]
    private static void SetVersionText(VersionUIHandler __instance) {
        var mainMenu = __instance.transform.parent.parent;
        var mainMenuAnimator = mainMenu.Find("MainMenuAnimation").GetComponent<UIAnimationController>();
        var originalFade = mainMenu.Find("MainMenuAnimation/VersionTextFadeIn");

        var label = __instance.versionText;
        var labelRect = label.rectTransform;

        var config = Plugin.Host.Services.GetRequiredService<Config>();

        CreateVersionLabel("SlopCrew Version", label, labelRect, out var versionLabel, out var versionRect);
        versionRect.anchoredPosition = labelRect.anchoredPosition + Vector2.up * label.fontSize;
        versionLabel.font = label.font;
        versionLabel.fontSize = label.fontSize;
        versionLabel.fontMaterial = label.fontSharedMaterial;
        versionLabel.SetText($"<color=\"purple\">SlopCrew v{PluginInfo.PLUGIN_VERSION} - ");
        // Need to do this to update renderedWidth for the next label
        versionLabel.ForceMeshUpdate();
        CreateLabelFade("SlopCrewVersionFade", mainMenuAnimator, originalFade, versionLabel);

        var interfaceUtility = Plugin.Host.Services.GetRequiredService<InterfaceUtility>();
        var username = PlayerNameFilter.DoFilter(config.General.Username.Value);

        CreateVersionLabel("SlopCrew Username", label, labelRect, out var usernameLabel, out var usernameRect);
        usernameRect.anchoredPosition = versionRect.anchoredPosition + new Vector2(versionLabel.renderedWidth + 8.0f, 2.0f);
        usernameLabel.font = interfaceUtility.NameplateFont;
        usernameLabel.fontMaterial = interfaceUtility.NameplateFontMaterial;
        usernameLabel.fontSize = label.fontSize;
        usernameLabel.SetText(username);
        CreateLabelFade("SlopCrewUsernameFade", mainMenuAnimator, originalFade, usernameLabel);

        // Reinitialize the main menu animator
        mainMenuAnimator.animationsInitialized = false;
        mainMenuAnimator.Awake();
    }

    private static void CreateLabelFade(string name, UIAnimationController mainMenuAnimator, Transform originalFade, TextMeshProUGUI label) {
        var originalFadeAnimation = originalFade.GetComponent<DOTweenAnimation>();

        var versionFade = new GameObject(name);
        versionFade.transform.SetParent(originalFade.parent);

        // Again, manual copy because Unity likes to mess up other stuff if we don't
        var versionFadeAnimation = versionFade.AddComponent<DOTweenAnimation>();
        versionFadeAnimation.targetGO = label.gameObject;
        versionFadeAnimation.target = label;
        versionFadeAnimation.targetIsSelf = originalFadeAnimation.targetIsSelf;
        versionFadeAnimation.tweenTargetIsTargetGO = originalFadeAnimation.tweenTargetIsTargetGO;
        versionFadeAnimation.animationType = originalFadeAnimation.animationType;
        versionFadeAnimation.targetType = originalFadeAnimation.targetType;
        versionFadeAnimation.isFrom = originalFadeAnimation.isFrom;
        versionFadeAnimation.autoKill = originalFadeAnimation.autoKill;
        versionFadeAnimation.autoPlay = originalFadeAnimation.autoPlay;
        versionFadeAnimation.duration = originalFadeAnimation.duration;
        versionFadeAnimation.delay = originalFadeAnimation.delay;
        versionFadeAnimation.endValueFloat = originalFadeAnimation.endValueFloat;
        versionFadeAnimation.isValid = true;
        versionFadeAnimation.Awake();
        mainMenuAnimator.animations.AddToArray(versionFadeAnimation);
    }

    private static void CreateVersionLabel(string name,
                                           TextMeshProUGUI originalLabel,
                                           RectTransform originalLabelRect,
                                           out TextMeshProUGUI label,
                                           out RectTransform rect) {
        // We have to manually copy the label because the game crashes otherwise for unknown reasons
        label = new GameObject(name).AddComponent<TextMeshProUGUI>();
        label.transform.SetParent(originalLabel.transform.parent, false);
        label.color = originalLabel.color;

        rect = label.rectTransform;
        rect.sizeDelta = originalLabelRect.sizeDelta;
        rect.anchorMin = originalLabelRect.anchorMin;
        rect.anchorMax = originalLabelRect.anchorMax;
        rect.offsetMin = originalLabelRect.offsetMin;
        rect.offsetMax = originalLabelRect.offsetMax;
    }
}
