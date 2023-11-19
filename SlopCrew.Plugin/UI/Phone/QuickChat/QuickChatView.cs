using Reptile;
using Reptile.Phone;
using SlopCrew.Common.Proto;
using TMPro;
using UnityEngine;

namespace SlopCrew.Plugin.UI.Phone;

public class QuickChatView : ExtendedPhoneScroll {
    private AppQuickChat? app;

    private const float ButtonScale = 2.0f;
    private const float IconScale = 2.0f;

    private const float ButtonSpacing = 16.0f;
    private const float ButtonTopMargin = -100.0f;

    public override void Initialize(App associatedApp, RectTransform root) {
        this.app = associatedApp as AppQuickChat;

        this.SCROLL_RANGE = AppSlopCrew.CategoryCount;
        this.SCROLL_AMOUNT = 1;
        this.OVERFLOW_BUTTON_AMOUNT = 1;
        this.SCROLL_DURATION = 0.25f;
        this.LIST_LOOPS = false;

        this.m_ButtonContainer = this.gameObject.GetComponent<RectTransform>();

        this.CreatePrefabs(AppSlopCrew.SpriteSheet);
    }

    private void CreatePrefabs(AppSpriteSheet spriteSheet) {
        var scaledButtonSize = AppSpriteSheet.ChatButtonSize * ButtonScale;

        var musicApp = app.MyPhone.GetAppInstance<AppMusicPlayer>();
        var musicButtonPrefab = musicApp.m_TrackList.m_AppButtonPrefab;

        var confirmArrow = musicButtonPrefab.transform.Find("PromptArrow");
        var titleLabel = musicButtonPrefab.transform.Find("TitleLabel").GetComponent<TextMeshProUGUI>();

        //m_AppButtonPrefab = button.gameObject;
        //m_AppButtonPrefab.SetActive(false);
    }

    public override void OnButtonCreated(PhoneScrollButton newButton) {
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
