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

    private const float ButtonSpacing = 40.0f;
    private static readonly float ButtonTopMargin = -120.0f;

    public CanvasGroup? CanvasGroup { get; private set; }

    public override void Initialize(App associatedApp, RectTransform root) {
        this.app = associatedApp as AppEncounters;

        this.SCROLL_RANGE = AppSlopCrew.EncounterCount;
        this.SCROLL_AMOUNT = 1;
        this.OVERFLOW_BUTTON_AMOUNT = 1;
        this.SCROLL_DURATION = 0.25f;
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
        rectTransform.SetAnchor(1.0f, 0.0f);
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
        var buttonTitle = Instantiate(titleLabel);
        buttonTitle.transform.SetParent(rectTransform, false);
        buttonTitle.transform.localPosition = new Vector2(scaledButtonSize.x * 0.119f, scaledButtonSize.y * 0.342f);
        buttonTitle.SetText("Encounter");

        // Mode description
        var buttonDescription = Instantiate(titleLabel);
        buttonDescription.transform.SetParent(rectTransform, false);
        buttonDescription.transform.localPosition =
            new Vector2(scaledButtonSize.x * 0.113f, scaledButtonSize.y * 0.111f);
        buttonDescription.SetText("Description");

        // Encounter status label
        var buttonStatus = Instantiate(titleLabel);
        buttonStatus.transform.SetParent(rectTransform, false);
        buttonStatus.transform.localPosition = new Vector2(scaledButtonSize.x * 0.0598f, -scaledButtonSize.y * 0.385f);
        buttonStatus.rectTransform.sizeDelta = new Vector2(scaledButtonSize.x * 0.95f, scaledButtonSize.y * 0.471f);
        buttonStatus.SetText("Status");
        buttonStatus.gameObject.SetActive(false);

        // Arrow to indicate pressing right = confirm
        var arrow = Instantiate(confirmArrow);
        arrow.transform.SetParent(rectTransform, false);
        arrow.transform.localPosition = new Vector2(scaledButtonSize.x - scaledButtonSize.x * 0.1f,
                                                           scaledButtonSize.y - scaledButtonSize.y * 0.1f);

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
