using Reptile;
using Reptile.Phone;
using SlopCrew.Common.Proto;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlopCrew.Plugin.UI.Phone;

public class SlopCrewScrollView : PhoneScroll {
    private AppSlopCrew? slopCrewApp;

    private Sprite? buttonSprite;
    private Sprite? buttonSpriteSelected;
    private Sprite?[] buttonIcons = new Sprite?[AppSlopCrew.EncounterCount];

    private static readonly Vector2Int SpriteSheetSize = new Vector2Int(1024, 1024);
    private const float SpriteSheetPadding = 16.0f;

    private static readonly Vector2 ButtonSpriteSize = new Vector2(512.0f, 167.0f);
    private static readonly Vector2 IconSpriteSize = new Vector2(116.0f, 101.0f);
    private static readonly float IconSpriteGridHeight = SpriteSheetSize.y - (ButtonSpriteSize.y + SpriteSheetPadding) * 2.0f - IconSpriteSize.y;

    private const float ButtonScale = 2.0f;
    private const float IconScale = 2.0f;

    private const float ButtonSpacing = 40.0f;
    private static readonly float ButtonTopMargin = -120.0f;

    public CanvasGroup? CanvasGroup { get; private set; }

    private void LoadSprites() {
        var phoneSheet =
            TextureLoader.LoadResourceAsTexture("SlopCrew.Plugin.res.phone_app_sheet.png", SpriteSheetSize.x, SpriteSheetSize.y, false, TextureWrapMode.Clamp);

        var centerPivot = new Vector2(0.5f, 0.5f);

        this.buttonSpriteSelected =
            Sprite.Create(phoneSheet, new Rect(0.0f, SpriteSheetSize.y - ButtonSpriteSize.y, ButtonSpriteSize.x, ButtonSpriteSize.y),
                          centerPivot, 100.0f);
        this.buttonSprite =
            Sprite.Create(phoneSheet, new Rect(0.0f, SpriteSheetSize.y - (ButtonSpriteSize.y * 2.0f) - SpriteSheetPadding, ButtonSpriteSize.x, ButtonSpriteSize.y),
                          centerPivot, 100.0f);

        var iconSizeWithPadding = IconSpriteSize.x + SpriteSheetPadding;
        this.buttonIcons[0] =
            Sprite.Create(phoneSheet, new Rect(0.0f, IconSpriteGridHeight, IconSpriteSize.x, IconSpriteSize.y),
                          centerPivot, 100.0f);
        this.buttonIcons[1] =
            Sprite.Create(phoneSheet, new Rect(iconSizeWithPadding, IconSpriteGridHeight, IconSpriteSize.x, IconSpriteSize.y),
                          centerPivot, 100.0f);
        this.buttonIcons[2] =
            Sprite.Create(phoneSheet, new Rect(iconSizeWithPadding * 2.0f, IconSpriteGridHeight, IconSpriteSize.x, IconSpriteSize.y),
                          centerPivot, 100.0f);
    }

    private void CreatePrefabs(GameObject arrowObject, TextMeshProUGUI titleObject) {
        var scaledButtonSize = ButtonSpriteSize * ButtonScale;
        var scaledIconSize = IconSpriteSize * IconScale;

        // Main button
        GameObject button = new GameObject("SlopCrew Button");
        var rectTransform = button.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(1.0f, 0.5f);
        rectTransform.anchorMax = rectTransform.anchorMin;
        rectTransform.sizeDelta = scaledButtonSize;

        CanvasGroup canvasGroup = button.AddComponent<CanvasGroup>();

        // Button background
        var buttonBackgroundObject = new GameObject("Button Background");
        buttonBackgroundObject.transform.SetParent(rectTransform, false);
        var buttonBackground = buttonBackgroundObject.AddComponent<Image>();
        buttonBackground.rectTransform.sizeDelta = scaledButtonSize;

        // Mode icon
        var buttonIconObject = new GameObject("Button Icon");
        buttonIconObject.transform.SetParent(rectTransform, false);
        var buttonIcon = buttonIconObject.AddComponent<Image>();
        buttonIcon.rectTransform.sizeDelta = scaledIconSize;
        buttonIcon.rectTransform.localPosition = new Vector2(-scaledButtonSize.x * 0.353f, scaledButtonSize.y * 0.111f);

        // Mode title
        var buttonTitle = Instantiate(titleObject);
        buttonTitle.transform.SetParent(rectTransform, false);
        buttonTitle.transform.localPosition = new Vector2(scaledButtonSize.x * 0.119f, scaledButtonSize.y * 0.342f);
        buttonTitle.SetText("Encounter");

        // Mode description
        var buttonDescription = Instantiate(titleObject);
        buttonDescription.transform.SetParent(rectTransform, false);
        buttonDescription.transform.localPosition =
            new Vector2(scaledButtonSize.x * 0.113f, scaledButtonSize.y * 0.111f);
        buttonDescription.SetText("Description");

        // Encounter status label
        var buttonStatus = Instantiate(titleObject);
        buttonStatus.transform.SetParent(rectTransform, false);
        buttonStatus.transform.localPosition = new Vector2(scaledButtonSize.x * 0.0598f, -scaledButtonSize.y * 0.385f);
        buttonStatus.rectTransform.sizeDelta = new Vector2(scaledButtonSize.x * 0.95f, scaledButtonSize.y * 0.471f);
        buttonStatus.SetText("Status");
        buttonStatus.gameObject.SetActive(false);

        // Arrow to indicate pressing right = confirm
        var confirmArrow = Instantiate(arrowObject);
        confirmArrow.transform.SetParent(rectTransform, false);
        confirmArrow.transform.localPosition = new Vector2(scaledButtonSize.x - scaledButtonSize.x * 0.1f,
                                                           scaledButtonSize.y - scaledButtonSize.y * 0.1f);

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
        var buttonSize = this.m_AppButtonPrefab.RectTransform().sizeDelta.y + ButtonSpacing;
        var rectTransform = button.RectTransform();

        var newPosition = new Vector2 {
            x = rectTransform.anchoredPosition.x,
            y = ButtonTopMargin - ((posIndex - (this.SCROLL_RANGE / 2.0f)) * buttonSize) -
                (this.SCROLL_RANGE % 2.0f == 0.0f ? buttonSize / 2.0f : 0.0f)
        };

        rectTransform.anchoredPosition = newPosition;
    }
}
