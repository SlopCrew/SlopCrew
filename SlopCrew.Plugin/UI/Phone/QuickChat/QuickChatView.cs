using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Reptile;
using Reptile.Phone;
using SlopCrew.Common;
using SlopCrew.Common.Proto;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlopCrew.Plugin.UI.Phone;

public class QuickChatView : ExtendedPhoneScroll {
    private AppQuickChat? app;

    private const float ButtonScale = 2.33f;
    private const float ButtonSpacing = 18.0f;
    private const float ButtonTopMargin = -ButtonSpacing;

    public override void Initialize(App associatedApp, RectTransform root) {
        this.app = associatedApp as AppQuickChat;

        this.SCROLL_RANGE = 9;
        this.SCROLL_AMOUNT = 1;
        this.OVERFLOW_BUTTON_AMOUNT = 1;
        this.SCROLL_DURATION = 0.1f;
        this.RESELECT_WAITS_ON_SCROLL = false;
        this.LIST_LOOPS = false;

        this.m_ButtonContainer = this.gameObject.GetComponent<RectTransform>();

        this.CreatePrefabs(AppSlopCrew.SpriteSheet);

        var messageCount = Constants.QuickChatMessages.Sum(x => x.Value.Count);

        this.InitalizeScrollView();
        this.SetListContent(messageCount);
    }

    private void CreatePrefabs(AppSpriteSheet spriteSheet) {
        var musicApp = app!.MyPhone.GetAppInstance<AppMusicPlayer>();
        var homeApp = app!.MyPhone.GetAppInstance<AppHomeScreen>();

        var musicButtonPrefab = musicApp.m_TrackList.m_AppButtonPrefab;
        var confirmArrow = homeApp.m_ScrollView.Selector.Arrow;
        var titleLabel = musicButtonPrefab.transform.Find("TitleLabel").GetComponent<TextMeshProUGUI>();

        var scaledButtonSize = AppSpriteSheet.ChatButtonSize * ButtonScale;

        // Main button
        GameObject button = new GameObject("QuickChat Button");
        var rectTransform = button.AddComponent<RectTransform>();
        // Align to the top
        rectTransform.SetAnchorAndPivot(1.0f, 1.0f);
        rectTransform.sizeDelta = scaledButtonSize;

        // Button background
        var buttonBackgroundObject = new GameObject("Button Background");
        buttonBackgroundObject.transform.SetParent(rectTransform, false);
        var buttonBackground = buttonBackgroundObject.AddComponent<Image>();
        buttonBackground.rectTransform.sizeDelta = scaledButtonSize;

        // Text
        var buttonText = Instantiate(titleLabel);
        var buttonTextRect = buttonText.rectTransform;
        buttonTextRect.SetParent(rectTransform, false);
        buttonTextRect.SetAnchorAndPivot(0.0f, 0.5f);
        buttonTextRect.sizeDelta = new Vector2(scaledButtonSize.x, scaledButtonSize.y);
        buttonTextRect.anchoredPosition = new Vector2(48.0f, 0.0f);
        buttonText.SetText("Message");

        var interfaceUtility = Plugin.Host.Services.GetRequiredService<InterfaceUtility>();
        buttonText.spriteAsset = interfaceUtility.EmojiAsset;

        // Arrow to indicate pressing right = confirm
        var arrow = Instantiate(confirmArrow).rectTransform;
        arrow!.SetParent(rectTransform, false);
        arrow!.SetAnchorAndPivot(1.0f, 0.5f);
        arrow!.anchoredPosition = new Vector2(-arrow.sizeDelta.x - 8.0f, 0.0f);

        var component = button.AddComponent<QuickChatButton>();
        component.InitializeButton(buttonBackground,
                                   buttonText,
                                   arrow.gameObject,
                                   spriteSheet.ChatSpriteNormal,
                                   spriteSheet.ChatSpriteSelected);

        this.m_AppButtonPrefab = button;
        this.m_AppButtonPrefab.SetActive(false);
    }

    public override void OnButtonCreated(PhoneScrollButton newButton) {
        newButton.gameObject.SetActive(true);
        base.OnButtonCreated(newButton);
    }

    public override void SetButtonContent(PhoneScrollButton button, int contentIndex) {
        var quickChatButton = (QuickChatButton) button;

        var categoryIndex = 0;
        foreach (var keyValuePair in Constants.QuickChatMessages.OrderBy(x => x.Key)) {
            var messageCount = keyValuePair.Value.Count;
            if (contentIndex >= messageCount) {
                contentIndex -= messageCount;
                categoryIndex++;
            } else {
                break;
            }
        }

        var category = (QuickChatCategory) categoryIndex;
        quickChatButton.SetButtonContents(category, contentIndex);
    }

    public override void SetButtonPosition(PhoneScrollButton button, float posIndex) {
        var rectTransform = button.RectTransform();
        rectTransform.anchoredPosition = this.GetButtonPosition(posIndex, rectTransform);
    }

    public Vector2 GetButtonPosition(float positionIndex, RectTransform rect) {
        var buttonSize = this.m_AppButtonPrefab.RectTransform().sizeDelta.y + ButtonSpacing;
        return new Vector2 {
            x = rect.anchoredPosition.x,
            y = ButtonTopMargin - (positionIndex * buttonSize)
        };
    }
}
