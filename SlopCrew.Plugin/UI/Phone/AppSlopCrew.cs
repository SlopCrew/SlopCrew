using Reptile;
using Reptile.Phone;
using System;
using SlopCrew.Common.Proto;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static SlopCrew.Plugin.UI.Phone.AppEncounters;
using DG.Tweening;

namespace SlopCrew.Plugin.UI.Phone;

public class AppSlopCrew : App {
    public enum Category {
        Chat,
        Encounters
    }

    public static readonly int EncounterCount = Enum.GetValues(typeof(EncounterType)).Length;
    public static readonly int CategoryCount = Enum.GetValues(typeof(Category)).Length;

    public static AppSpriteSheet SpriteSheet;

    private SlopCrewScrollView? scrollView;

    public override void Awake() {
        // Cannot happen in static initializer; debug builds of Unity flag this as an error and crash
        if (AppSlopCrew.SpriteSheet == null) {
            AppSlopCrew.SpriteSheet = new AppSpriteSheet(EncounterCount, CategoryCount);
        }
}
    
    public override void OnAppInit() {
        this.m_Unlockables = Array.Empty<AUnlockable>();

        this.scrollView = ExtendedPhoneScroll.Create<SlopCrewScrollView>("Categories", this, this.Content);

        var musicApp = this.MyPhone.GetAppInstance<AppMusicPlayer>();
        AppUtility.CreateAppOverlay(musicApp, false, this.Content, "Slop Crew", SpriteSheet.MainIcon, out _, out _,
                                    this.scrollView.RectTransform());
    }

    public override void OnAppEnable() {
        this.PlayEnableAnimation();
    }

    private void PlayEnableAnimation() {
        Sequence enableAnimation = DOTween.Sequence();

        for (int i = 0; i < this.scrollView!.GetScrollRange(); i++) {
            var button = (SlopCrewButton) this.scrollView.GetButtonByRelativeIndex(i);
            RectTransform buttonRect = button.RectTransform();

            var targetPosition = this.scrollView.GetButtonPosition(i, buttonRect);
            if (i != this.scrollView.GetSelectorPos())
                targetPosition.x = 70.0f;
            button.ToggleBackground(true);
            buttonRect.anchoredPosition = new Vector2(this.MyPhone.ScreenSize.x, buttonRect.anchoredPosition.y);
            enableAnimation.Append(buttonRect.DOAnchorPos(targetPosition, 0.08f));
        }
    }

    public override void OnPressUp() {
        this.scrollView!.Move(PhoneScroll.ScrollDirection.Previous, this.m_AudioManager);
    }

    public override void OnPressDown() {
        this.scrollView!.Move(PhoneScroll.ScrollDirection.Next, this.m_AudioManager);
    }

    public override void OnPressRight() {
        if (this.scrollView!.SelectedButtton == null)
            return;
        this.scrollView.HoldAnimationSelectedButton();
    }

    public override void OnReleaseRight() {
        int contentIndex = this.scrollView!.GetContentIndex();
        var selectedCategory = (Category) contentIndex;

        this.PlayAppSelectedAnimation(selectedCategory);

        this.scrollView!.ActivateAnimationSelectedButton();
        this.m_AudioManager.PlaySfxUI(SfxCollectionID.PhoneSfx, AudioClipID.FlipPhone_Confirm);
    }

    private void PlayAppSelectedAnimation(Category selectedCategory) {
        Sequence disableAnimation = DOTween.Sequence();

        for (int i = 0; i < this.scrollView!.GetScrollRange(); i++) {
            var button = (SlopCrewButton) this.scrollView.GetButtonByRelativeIndex(i);
            RectTransform buttonRect = button.RectTransform();
            if (i != this.scrollView.GetSelectorPos()) {
                button.ToggleBackground(false);
                disableAnimation.Append(buttonRect.DOAnchorPosX(-this.MyPhone.ScreenSize.x, 0.1f));
            }
        }
        disableAnimation.AppendCallback(() => this.OpenApp(selectedCategory));
    }

    private void OpenApp(Category app) {
        switch (app) {
            case Category.Chat:
                this.MyPhone.OpenApp(typeof(AppQuickChat));
                break;
            case Category.Encounters:
                this.MyPhone.OpenApp(typeof(AppEncounters));
                break;
        }
    }
}
