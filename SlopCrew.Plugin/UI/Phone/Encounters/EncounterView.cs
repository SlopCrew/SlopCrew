using Google.Protobuf.WellKnownTypes;
using Reptile;
using Reptile.Phone;
using SlopCrew.Common.Proto;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlopCrew.Plugin.UI.Phone;

public class EncounterView : ExtendedPhoneScroll {
    private AppEncounters? app;

    private const float ButtonScale = 2.0f;
    private const float IconScale = 2.0f;

    private const float ButtonSpacing = 36.0f;
    private static readonly float ButtonTopMargin = -ButtonSpacing;

    public CanvasGroup? CanvasGroup { get; private set; }

    public override void Initialize(App associatedApp, RectTransform root) {
        this.app = associatedApp as AppEncounters;

        this.SCROLL_RANGE = 3;
        this.SCROLL_AMOUNT = 1;
        this.OVERFLOW_BUTTON_AMOUNT = 1;
        this.SCROLL_DURATION = 0.1f;
        this.RESELECT_WAITS_ON_SCROLL = false;
        this.LIST_LOOPS = false;

        this.m_ButtonContainer = this.gameObject.GetComponent<RectTransform>();
        this.CanvasGroup = this.gameObject.AddComponent<CanvasGroup>();

        CreatePrefabs(AppSlopCrew.SpriteSheet);

        InitalizeScrollView();
        SetListContent(AppSlopCrew.EncounterCount);
    }

    private void CreatePrefabs(AppSpriteSheet spriteSheet) {
        var musicApp = app!.MyPhone.GetAppInstance<AppMusicPlayer>();
        var musicButtonPrefab = musicApp.m_TrackList.m_AppButtonPrefab;

        var confirmArrow = musicButtonPrefab.transform.Find("PromptArrow");
        var titleLabel = musicButtonPrefab.transform.Find("TitleLabel").GetComponent<TextMeshProUGUI>();

        var scaledButtonSize = AppSpriteSheet.EncounterButtonSize * ButtonScale;
        var scaledIconSize = AppSpriteSheet.EncounterIconSize * IconScale;

        // Main button
        GameObject button = new GameObject("Encounter Button");
        var rectTransform = button.AddComponent<RectTransform>();
        // Align to the top
        rectTransform.SetAnchorAndPivot(1.0f, 1.0f);
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
        var buttonIconRect = buttonIcon.rectTransform;
        buttonIconRect.sizeDelta = scaledIconSize;
        buttonIconRect.SetAnchor(0.0f, 0.5f);
        buttonIconRect.anchoredPosition = new Vector2(150.0f, 40.0f);

        var buttonTextX = (buttonIconRect.anchoredPosition.x + buttonIconRect.sizeDelta.x * 0.5f) + 16.0f;

        // Mode title
        var buttonTitle = Instantiate(titleLabel);
        var buttonTitleRect = buttonTitle.rectTransform;
        buttonTitleRect.SetParent(rectTransform, false);
        buttonTitleRect.SetAnchorAndPivot(0.0f, 0.5f);
        buttonTitleRect.sizeDelta = new Vector2(scaledButtonSize.x - buttonTextX, buttonTitle.fontSize);
        buttonTitleRect.anchoredPosition = new Vector2(buttonTextX, 100.0f);
        buttonTitle.SetText("Encounter");

        // Mode description
        var buttonDescription = Instantiate(buttonTitle);
        var buttonDescriptionRect = buttonDescription.rectTransform;
        buttonDescriptionRect.SetParent(rectTransform, false);
        buttonDescriptionRect.anchoredPosition = new Vector2(buttonTextX, 32.0f);
        buttonDescription.SetText("Description");

        // Encounter status label
        var buttonStatus = Instantiate(buttonTitle);
        var buttonStatusRect = buttonStatus.rectTransform;
        buttonStatusRect.SetParent(rectTransform, false);
        buttonStatusRect.sizeDelta = new Vector2(scaledButtonSize.x - 64.0f, buttonStatus.fontSize);
        buttonStatusRect.anchoredPosition = new Vector2(64.0f, -130.0f);
        buttonStatus.SetText("Status");
        buttonStatus.gameObject.SetActive(false);

        // Arrow to indicate pressing right = confirm
        var arrow = (RectTransform) Instantiate(confirmArrow);
        arrow.SetParent(rectTransform, false);
        arrow.SetAnchorAndPivot(1.0f, 1.0f);
        arrow.anchoredPosition = Vector2.one * -32.0f;

        var slopCrewButton = button.AddComponent<EncounterButton>();
        slopCrewButton.InitializeButton(canvasGroup,
                                        buttonBackground,
                                        buttonIcon,
                                        buttonTitle,
                                        buttonDescription,
                                        buttonStatus,
                                        arrow.gameObject,
                                        spriteSheet.EncounterButtonSpriteNormal!,
                                        spriteSheet.EncounterButtonSpriteSelected!);

        m_AppButtonPrefab = slopCrewButton.gameObject;
        m_AppButtonPrefab.SetActive(false);
    }

    public override void OnButtonCreated(PhoneScrollButton newButton) {
        var slopCrewButton = (EncounterButton) newButton;
        if (slopCrewButton != null) slopCrewButton.SetApp(this.app!);

        newButton.gameObject.SetActive(true);
        base.OnButtonCreated(newButton);
    }

    public override void SetButtonContent(PhoneScrollButton button, int contentIndex) {
        var slopCrewButton = (EncounterButton) button;
        var encounterType = (EncounterType) contentIndex;
        slopCrewButton.SetButtonContents(encounterType, AppSlopCrew.SpriteSheet!.GetEncounterIcon(encounterType)!);
    }

    public override void SetButtonPosition(PhoneScrollButton button, float posIndex) {
        var rectTransform = button.RectTransform();
        rectTransform.anchoredPosition = GetButtonPosition(posIndex);
    }

    public Vector2 GetButtonPosition(float positionIndex) {
        var buttonSize = this.m_AppButtonPrefab.RectTransform().sizeDelta.y + ButtonSpacing;
        return new Vector2 {
            x = 0.0f,
            y = ButtonTopMargin - (positionIndex * buttonSize)
        };
    }
}
