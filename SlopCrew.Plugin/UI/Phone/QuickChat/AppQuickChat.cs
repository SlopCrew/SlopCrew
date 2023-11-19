using Reptile;
using Reptile.Phone;
using System;
using UnityEngine;
using DG.Tweening;
using SlopCrew.Common.Proto;

namespace SlopCrew.Plugin.UI.Phone;

public class AppQuickChat : App {
    public static readonly int MessageCategoryCount = Enum.GetValues(typeof(QuickChatCategory)).Length;

    private QuickChatView? scrollView;

    public override void OnAppInit() {
        this.m_Unlockables = Array.Empty<AUnlockable>();

        this.scrollView = ExtendedPhoneScroll.Create<QuickChatView>("Categories", this, Content);

        var musicApp = this.MyPhone.GetAppInstance<AppMusicPlayer>();
        AppUtility.CreateAppOverlay(musicApp, false, Content, "Quick Chat", AppSlopCrew.SpriteSheet.MainIcon, out _, out _, scrollView.RectTransform());
    }

    public override void OnAppEnable() {
        PlayEnableAnimation();
    }

    private void PlayEnableAnimation() {
        Sequence enableAnimation = DOTween.Sequence();

        for (int i = 0; i < scrollView!.GetScrollRange(); i++) {
            var button = (QuickChatButton) scrollView.GetButtonByRelativeIndex(i);
            RectTransform buttonRect = button.RectTransform();

            var targetPosition = scrollView.GetButtonPosition(i);
            if (i != scrollView.GetSelectorPos()) targetPosition.x = 70.0f;
            buttonRect.anchoredPosition = new Vector2(MyPhone.ScreenSize.x, buttonRect.anchoredPosition.y);
            enableAnimation.Append(buttonRect.DOAnchorPos(targetPosition, 0.08f));
        }
    }

    public override void OnPressUp() {
        scrollView!.Move(PhoneScroll.ScrollDirection.Previous, m_AudioManager);
    }

    public override void OnPressDown() {
        scrollView!.Move(PhoneScroll.ScrollDirection.Next, m_AudioManager);
    }

    public override void OnPressRight() {
        scrollView!.HoldAnimationSelectedButton();
    }

    public override void OnReleaseRight() {
        int contentIndex = scrollView!.GetContentIndex();
        var button = (QuickChatButton) scrollView.GetButtonByRelativeIndex(contentIndex);

        button.GetMessage(out QuickChatCategory category, out int index);
        SendQuickChatMessage(category, index);

        scrollView!.ActivateAnimationSelectedButton();
        m_AudioManager.PlaySfxUI(SfxCollectionID.PhoneSfx, AudioClipID.FlipPhone_Confirm);
    }

    private void SendQuickChatMessage(QuickChatCategory category, int index) {
        // TODO
        // Send the message packet, make the text pop up etc.
    }
}
