using Reptile;
using Reptile.Phone;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlopCrew.Plugin.UI.Phone;

internal class SlopCrewScrollView : ExtendedPhoneScroll {
    private AppSlopCrew? app;

    private const float ButtonScale = 2.5f;
    private const float IconScale = 2.0f;

    private const float ButtonSpacing = 24.0f;
    private const float ButtonTopMargin = -ButtonSpacing;

    public override void Initialize(App associatedApp, RectTransform root) {
        this.app = associatedApp as AppSlopCrew;

        this.SCROLL_RANGE = AppSlopCrew.CategoryCount;
        this.SCROLL_AMOUNT = 1;
        this.OVERFLOW_BUTTON_AMOUNT = 1;
        this.SCROLL_DURATION = 0.1f;
        this.RESELECT_WAITS_ON_SCROLL = false;
        this.LIST_LOOPS = false;

        this.m_ButtonContainer = this.gameObject.GetComponent<RectTransform>();

        this.CreatePrefabs(AppSlopCrew.SpriteSheet);

        InitalizeScrollView();
        SetListContent(AppSlopCrew.CategoryCount);
    }

    private void CreatePrefabs(AppSpriteSheet spriteSheet) {
        var musicApp = app!.MyPhone.GetAppInstance<AppMusicPlayer>();
        var homeApp = app!.MyPhone.GetAppInstance<AppHomeScreen>();

        var musicButtonPrefab = musicApp.m_TrackList.m_AppButtonPrefab;
        var confirmArrow = homeApp.m_ScrollView.Selector.Arrow;
        var titleLabel = musicButtonPrefab.transform.Find("TitleLabel").GetComponent<TextMeshProUGUI>();

        var scaledButtonSize = AppSpriteSheet.CategoryButtonSize * ButtonScale;
        var scaledIconSize = AppSpriteSheet.CategoryIconSize * IconScale;

        // Main button
        GameObject button = new GameObject("Category Button");
        var rectTransform = button.AddComponent<RectTransform>();
        // Align to the top
        rectTransform.SetAnchorAndPivot(1.0f, 1.0f);
        rectTransform.sizeDelta = scaledButtonSize;

        // Button background
        var buttonBackgroundObject = new GameObject("Button Background");
        buttonBackgroundObject.transform.SetParent(rectTransform, false);
        var buttonBackground = buttonBackgroundObject.AddComponent<Image>();
        buttonBackground.rectTransform.sizeDelta = scaledButtonSize;

        // Icon
        var buttonIconObject = new GameObject("Button Icon");
        buttonIconObject.transform.SetParent(rectTransform, false);
        var buttonIcon = buttonIconObject.AddComponent<Image>();
        var buttonIconRect = buttonIcon.rectTransform;
        buttonIconRect.SetAnchor(0.0f, 0.5f);
        buttonIconRect.sizeDelta = scaledIconSize;
        buttonIconRect.anchoredPosition = new Vector2((scaledIconSize.x * 0.5f) + 32.0f, 0.0f);

        // Title
        var buttonTitle = Instantiate(titleLabel);
        var buttonTitleRect = buttonTitle.rectTransform;
        buttonTitleRect.SetParent(rectTransform, false);
        buttonTitleRect.SetAnchorAndPivot(0.0f, 0.5f);
        float textSize = buttonIconRect.anchoredPosition.x + (buttonIconRect.sizeDelta.x * 0.5f) + 8.0f;
        buttonTitleRect.sizeDelta = new Vector2(scaledButtonSize.x - textSize, scaledButtonSize.y);
        buttonTitleRect.anchoredPosition = new Vector2(textSize, 0.0f);
        buttonTitle.SetText("Category");

        // Arrow to indicate pressing right = confirm
        var arrow = Instantiate(confirmArrow).rectTransform;
        arrow!.SetParent(rectTransform, false);
        arrow!.SetAnchorAndPivot(1.0f, 0.5f);
        arrow!.anchoredPosition = new Vector2(-arrow.sizeDelta.x - 8.0f, 0.0f);

        var component = button.AddComponent<SlopCrewButton>();
        component.InitializeButton(buttonBackground,
                                   buttonIcon,
                                   buttonTitle,
                                   arrow.gameObject,
                                   spriteSheet.CategoryButtonNormal,
                                   spriteSheet.CategoryButtonSelected);

        m_AppButtonPrefab = button;
        m_AppButtonPrefab.SetActive(false);
    }

    public override void OnButtonCreated(PhoneScrollButton newButton) {
        newButton.gameObject.SetActive(true);

        base.OnButtonCreated(newButton);
    }

    public override void SetButtonContent(PhoneScrollButton button, int contentIndex) {
        var slopCrewButton = (SlopCrewButton) button;
        var categoryType = (AppSlopCrew.Category) contentIndex;
        slopCrewButton.SetButtonContents(categoryType, AppSlopCrew.SpriteSheet.GetCategoryIcon(categoryType)!);
    }

    public override void SetButtonPosition(PhoneScrollButton button, float posIndex) {
        var rectTransform = button.RectTransform();
        rectTransform.anchoredPosition = GetButtonPosition(posIndex, rectTransform);
    }

    public Vector2 GetButtonPosition(float positionIndex, RectTransform rect) {
        var buttonSize = this.m_AppButtonPrefab.RectTransform().sizeDelta.y + ButtonSpacing;
        return new Vector2 {
            x = rect.anchoredPosition.x,
            y = ButtonTopMargin - (positionIndex * buttonSize)
        };
    }
}
