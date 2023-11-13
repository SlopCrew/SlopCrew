using Reptile;
using Reptile.Phone;
using SlopCrew.Common.Proto;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlopCrew.Plugin.UI.Phone;

public class SlopCrewScrollView : PhoneScroll {
    private float buttonSpacing = 55.0f;
    private float buttonTopMargin = -110.0f;
    private AppSlopCrew? slopCrewApp;

    private Sprite? buttonSprite;
    private Sprite? buttonSpriteSelected;
    private Sprite?[] buttonIcons = new Sprite?[3];

    public CanvasGroup? CanvasGroup { get; private set; }

    private void LoadSprites() {
        buttonSprite = TextureLoader.LoadResourceAsSprite("SlopCrew.Plugin.res.phone_main_button.png", 530, 190);
        buttonSpriteSelected =
            TextureLoader.LoadResourceAsSprite("SlopCrew.Plugin.res.phone_main_button_selected.png", 530, 190);

        buttonIcons[0] = TextureLoader.LoadResourceAsSprite("SlopCrew.Plugin.res.phone_icon_score.png", 128, 128);
        buttonIcons[1] = buttonIcons[0];
        buttonIcons[2] = TextureLoader.LoadResourceAsSprite("SlopCrew.Plugin.res.phone_icon_race.png", 128, 128);
    }

    private void CreatePrefabs(GameObject arrowObject, TextMeshProUGUI titleObject) {
        // Main button
        var button = new GameObject("SlopCrew Button");
        var rectTransform = button.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(1.0f, 0.5f);
        rectTransform.anchorMax = rectTransform.anchorMin;
        rectTransform.sizeDelta = new Vector2(1052.0f, 325.0f);

        var canvasGroup = button.AddComponent<CanvasGroup>();

        // Button background
        var buttonBackgroundObject = new GameObject("Button Background");
        buttonBackgroundObject.transform.SetParent(rectTransform, false);
        var buttonBackground = buttonBackgroundObject.AddComponent<Image>();
        buttonBackground.rectTransform.sizeDelta = new Vector2(1052.0f, 325.0f);

        // Mode icon
        var buttonIconObject = new GameObject("Button Icon");
        buttonIconObject.transform.SetParent(rectTransform, false);
        var buttonIcon = buttonIconObject.AddComponent<Image>();
        buttonIcon.rectTransform.sizeDelta = new Vector2(150.0f, 150.0f);
        buttonIcon.rectTransform.localPosition = new Vector2(-375.0f, 35.0f);

        // Mode title
        var buttonTitle = Instantiate(titleObject);
        buttonTitle.transform.SetParent(rectTransform, false);
        buttonTitle.transform.localPosition = new Vector2(96.0f, 76.0f);
        buttonTitle.SetText("Encounter");

        // Mode title
        var buttonDescription = Instantiate(titleObject);
        buttonDescription.transform.SetParent(rectTransform, false);
        buttonDescription.transform.localPosition = new Vector2(96.0f, 0.0f);
        buttonDescription.SetText("Description");

        // Encounter status label
        var buttonStatus = Instantiate(titleObject);
        buttonStatus.transform.SetParent(rectTransform, false);
        buttonStatus.transform.localPosition = new Vector2(-48.0f, -110.0f);
        buttonStatus.rectTransform.sizeDelta = new Vector2(750.0f, 165.2f);
        buttonStatus.SetText("Status");
        buttonStatus.gameObject.SetActive(false);

        // Arrow to indicate pressing right = confirm
        var confirmArrow = Instantiate(arrowObject);
        confirmArrow.transform.SetParent(rectTransform, false);
        confirmArrow.transform.localPosition = new Vector2(430.0f, 120.0f);

        var slopCrewButton = button.AddComponent<SlopCrewButton>();
        slopCrewButton.InitializeButton(canvasGroup,
                                        buttonBackground,
                                        buttonIcon,
                                        buttonTitle,
                                        buttonDescription,
                                        buttonStatus,
                                        confirmArrow,
                                        buttonSprite!,
                                        buttonSpriteSelected!);

        m_AppButtonPrefab = slopCrewButton.gameObject;
        m_AppButtonPrefab.SetActive(false);
    }

    public void Initialize(AppSlopCrew app, GameObject arrowObject, TextMeshProUGUI titleObject) {
        this.slopCrewApp = app;

        this.SCROLL_RANGE = AppSlopCrew.EncounterCount;
        this.SCROLL_AMOUNT = 1;
        this.OVERFLOW_BUTTON_AMOUNT = 1;
        this.SCROLL_DURATION = 0.25f;
        this.LIST_LOOPS = false;

        this.m_ButtonContainer = this.gameObject.GetComponent<RectTransform>();
        this.CanvasGroup = this.gameObject.AddComponent<CanvasGroup>();

        this.LoadSprites();
        this.CreatePrefabs(arrowObject, titleObject);
    }

    public override void OnButtonCreated(PhoneScrollButton newButton) {
        var slopCrewButton = (SlopCrewButton) newButton;
        if (slopCrewButton != null) slopCrewButton.SetApp(this.slopCrewApp!);

        newButton.gameObject.SetActive(true);
        base.OnButtonCreated(newButton);
    }

    public override void SetButtonContent(PhoneScrollButton button, int contentIndex) {
        var slopCrewButton = (SlopCrewButton) button;
        slopCrewButton.SetButtonContents((EncounterType) contentIndex, this.buttonIcons[contentIndex]!);
    }

    public override void SetButtonPosition(PhoneScrollButton button, float posIndex) {
        var buttonSize = this.m_AppButtonPrefab.RectTransform().sizeDelta.y + this.buttonSpacing;
        var rectTransform = button.RectTransform();

        var newPosition = new Vector2 {
            x = rectTransform.anchoredPosition.x,
            y = this.buttonTopMargin - ((posIndex - (this.SCROLL_RANGE / 2.0f)) * buttonSize) -
                (this.SCROLL_RANGE % 2.0f == 0.0f ? buttonSize / 2.0f : 0.0f)
        };

        rectTransform.anchoredPosition = newPosition;
    }
}
