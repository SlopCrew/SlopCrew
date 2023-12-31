using Reptile;
using Reptile.Phone;
using System;
using UnityEngine;
using DG.Tweening;
using Microsoft.Extensions.DependencyInjection;
using SlopCrew.Common.Proto;

namespace SlopCrew.Plugin.UI.Phone;

public class AppQuickChat : App {
    public static readonly int MessageCategoryCount = Enum.GetValues(typeof(QuickChatCategory)).Length;

    private QuickChatView? scrollView;
    private ConnectionManager? connectionManager;
    private float continuousScrollTimer;

    public override void OnAppInit() {
        this.m_Unlockables = Array.Empty<AUnlockable>();

        this.scrollView = ExtendedPhoneScroll.Create<QuickChatView>("Messages", this, this.Content);

        var musicApp = this.MyPhone.GetAppInstance<AppMusicPlayer>();
        AppUtility.CreateAppOverlay(musicApp, false, Content, "Quick Chat", AppSlopCrew.SpriteSheet.MainIcon, out _,
                                    out _, this.scrollView.RectTransform());
    }

    public override void OnAppEnable() {
        this.connectionManager = Plugin.Host.Services.GetRequiredService<ConnectionManager>();
        this.PlayEnableAnimation();
    }

    private void PlayEnableAnimation() {
        Sequence enableAnimation = DOTween.Sequence();

        for (int i = 0; i < this.scrollView!.GetScrollRange(); i++) {
            var button = (QuickChatButton) this.scrollView.GetButtonByRelativeIndex(i);
            RectTransform buttonRect = button.RectTransform();

            var targetPosition = this.scrollView.GetButtonPosition(i, buttonRect);
            if (i != this.scrollView.GetSelectorPos())
                targetPosition.x = 70.0f;
            buttonRect.anchoredPosition = new Vector2(MyPhone.ScreenSize.x, buttonRect.anchoredPosition.y);
            enableAnimation.Append(buttonRect.DOAnchorPos(targetPosition, 0.08f));
        }
    }

    public override void OnPressUp() {
        this.scrollView!.Move(PhoneScroll.ScrollDirection.Previous, this.m_AudioManager);
        this.continuousScrollTimer -= 0.4f;
    }

    public override void OnPressDown() {
        this.scrollView!.Move(PhoneScroll.ScrollDirection.Next, this.m_AudioManager);
        this.continuousScrollTimer -= 0.4f;
    }

    public override void OnReleaseUp() => this.continuousScrollTimer = 0.0f;
    public override void OnReleaseDown() => this.continuousScrollTimer = 0.0f;

    public override void OnHoldUp() {
        this.continuousScrollTimer += Core.dt;
        if (this.continuousScrollTimer < 0.1f)
            return;
        this.continuousScrollTimer = 0.0f;
        this.scrollView!.Move(PhoneScroll.ScrollDirection.Previous, this.m_AudioManager);
    }

    public override void OnHoldDown() {
        this.continuousScrollTimer += Core.dt;
        if (this.continuousScrollTimer < 0.1f)
            return;
        this.continuousScrollTimer = 0.0f;
        this.scrollView!.Move(PhoneScroll.ScrollDirection.Next, this.m_AudioManager);
    }

    public override void OnPressRight() {
        if (this.scrollView!.SelectedButtton == null)
            return;
        this.scrollView.HoldAnimationSelectedButton();
    }

    public override void OnReleaseRight() {
        var button = (QuickChatButton) this.scrollView!.SelectedButtton;

        button.GetMessage(out var category, out var index);
        this.SendQuickChatMessage(category, index);

        this.scrollView!.ActivateAnimationSelectedButton();
        this.m_AudioManager.PlaySfxUI(SfxCollectionID.PhoneSfx, AudioClipID.FlipPhone_Confirm);
    }

    private void SendQuickChatMessage(QuickChatCategory category, int index) {
        this.connectionManager?.SendMessage(new ServerboundMessage {
            QuickChat = new ServerboundQuickChat {
                QuickChat = new QuickChat {
                    Category = category,
                    Index = index
                }
            }
        });

        QuickChatUtility.SpawnQuickChat(WorldHandler.instance.GetCurrentPlayer(), category, index);
    }
}
