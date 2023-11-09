
using BepInEx.Logging;
using HarmonyLib;
using Reptile;
using Reptile.Phone;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlopCrew.Plugin.UI.Phone;

public class SlopCrewScrollView : PhoneScroll {
    private float buttonSpacing = 65.0f;
    private float buttonTopMargin = -75.0f;
    private AppSlopCrew? slopCrewApp;

    private Sprite? buttonSprite;
    private Sprite? buttonSpriteSelected;
    private Sprite?[] buttonIcons = new Sprite?[3];

    private static readonly string[] ModeButtonTitles = {
        "Score Battle",
        "Combo Battle",
        "Race"
    };

    private void LoadSprites() {
        buttonSprite = TextureLoader.LoadResourceAsSprite("SlopCrew.Plugin.res.phone_main_button.png", 530, 150);
        buttonSpriteSelected = TextureLoader.LoadResourceAsSprite("SlopCrew.Plugin.res.phone_main_button_selected.png", 530, 150);

        buttonIcons[0] = TextureLoader.LoadResourceAsSprite("SlopCrew.Plugin.res.phone_icon_score.png", 128, 128);
        buttonIcons[1] = buttonIcons[0];
        buttonIcons[2] = TextureLoader.LoadResourceAsSprite("SlopCrew.Plugin.res.phone_icon_race.png", 128, 128);
    }

    private void CreatePrefabs(GameObject arrowObject, TextMeshProUGUI titleObject) {
        GameObject button = new GameObject("SlopCrew Button");
        var rectTransform = button.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(1.0f, 0.5f);
        rectTransform.anchorMax = rectTransform.anchorMin;
        rectTransform.sizeDelta = new Vector2(1052.0f, 298.0f);

        var buttonBackgroundObject = new GameObject("Button Background");
        buttonBackgroundObject.transform.SetParent(rectTransform, false);
        var buttonBackground = buttonBackgroundObject.AddComponent<Image>();
        buttonBackground.rectTransform.sizeDelta = new Vector2(1052.0f, 298.0f);

        var buttonIconObject = new GameObject("Button Icon");
        buttonIconObject.transform.SetParent(rectTransform, false);
        var buttonIcon = buttonIconObject.AddComponent<Image>();
        buttonIcon.rectTransform.sizeDelta = new Vector2(150.0f, 150.0f);
        buttonIcon.rectTransform.localPosition = new Vector2(-375.0f, 0.0f);

        var buttonTitle = Instantiate(titleObject);
        buttonTitle.transform.SetParent(rectTransform, false);
        buttonTitle.transform.localPosition = new Vector2(96.0f, 76.0f);
        buttonTitle.SetText("Slop Crew Encounter");

        var confirmArrow = Instantiate(arrowObject);
        confirmArrow.transform.SetParent(rectTransform, false);
        confirmArrow.transform.localPosition = new Vector2(430.0f, 120.0f);

        var slopCrewButton = button.AddComponent<SlopCrewButton>();
        slopCrewButton.InitializeButton(buttonBackground, buttonIcon, buttonTitle, confirmArrow, buttonSprite, buttonSpriteSelected);

        m_AppButtonPrefab = slopCrewButton.gameObject;
        m_AppButtonPrefab.SetActive(false);
    }

    public void Initialize(AppSlopCrew app, GameObject arrowObject, TextMeshProUGUI titleObject) {
        var traverse = Traverse.Create(this);

        slopCrewApp = app;

        SCROLL_RANGE = 3;
        SCROLL_AMOUNT = 1;
        OVERFLOW_BUTTON_AMOUNT = 1;
        SCROLL_DURATION = 0.25f;
        LIST_LOOPS = false;

        traverse.Field("m_ButtonContainer").SetValue(gameObject.GetComponent<RectTransform>());

        LoadSprites();
        CreatePrefabs(arrowObject, titleObject);
    }

    protected override void OnButtonCreated(PhoneScrollButton newButton) {
        newButton.gameObject.SetActive(true);
        base.OnButtonCreated(newButton);
    }

    protected override void SetButtonContent(PhoneScrollButton button, int contentIndex) {
        (button as SlopCrewButton).SetButtonContents(ModeButtonTitles[contentIndex], buttonIcons[contentIndex]);
    }

    protected override void SetButtonPosition(PhoneScrollButton button, float posIndex) {
        float buttonSize = m_AppButtonPrefab.RectTransform().sizeDelta.y + this.buttonSpacing;
        RectTransform rectTransform = button.RectTransform();

        Vector2 newPosition = new Vector2() {
            x = rectTransform.anchoredPosition.x,
            y = buttonTopMargin - (posIndex - (SCROLL_RANGE / 2.0f)) * buttonSize - (SCROLL_RANGE % 2.0f == 0.0f ? buttonSize / 2.0f : 0.0f)
        };

        rectTransform.anchoredPosition = newPosition;
    }
}
