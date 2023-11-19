using Reptile;
using Reptile.Phone;
using System;
using TMPro;
using UnityEngine.UIElements;
using UnityEngine;

namespace SlopCrew.Plugin.UI.Phone;

public class AppQuickChat : App {
    private QuickChatView? scrollView;

    public override void OnAppInit() {
        this.m_Unlockables = Array.Empty<AUnlockable>();

        this.scrollView = ExtendedPhoneScroll.Create<QuickChatView>("Categories", this, Content);

        var musicApp = this.MyPhone.GetAppInstance<AppMusicPlayer>();
        AppUtility.CreateAppOverlay(musicApp, false, Content, "Quick Chat", AppSlopCrew.SpriteSheet.MainIcon, out _, out _, scrollView.RectTransform());
    }
}
