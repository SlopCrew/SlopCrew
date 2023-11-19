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

    public static readonly AppSpriteSheet SpriteSheet = new AppSpriteSheet(EncounterCount, CategoryCount);

    private SlopCrewScrollView? scrollView;

    public override void OnAppInit() {
        this.m_Unlockables = Array.Empty<AUnlockable>();

        this.scrollView = ExtendedPhoneScroll.Create<SlopCrewScrollView>("Categories", this, Content);

        var musicApp = this.MyPhone.GetAppInstance<AppMusicPlayer>();
        AppUtility.CreateAppOverlay(musicApp, false, Content, "Slop Crew", SpriteSheet.MainIcon, out _, out _, scrollView.RectTransform());
    }

    public override void OnAppEnable() {
        PlayEnableAnimation();
    }

    private void PlayEnableAnimation() {
        Sequence enableAnimation = DOTween.Sequence();

        for (int i = 0; i < scrollView!.GetScrollRange(); i++) {
            var button = (SlopCrewButton) scrollView.GetButtonByRelativeIndex(i);
            RectTransform buttonRect = button.RectTransform();

            var targetPosition = scrollView.GetButtonPosition(i);
            if (i != scrollView.GetSelectorPos()) targetPosition.x = 70.0f;
            button.ToggleBackground(true);
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
        var selectedCategory = (Category) contentIndex;

        PlayAppSelectedAnimation(selectedCategory);

        scrollView!.ActivateAnimationSelectedButton();
        m_AudioManager.PlaySfxUI(SfxCollectionID.PhoneSfx, AudioClipID.FlipPhone_Confirm);
    }

    private void PlayAppSelectedAnimation(Category selectedCategory) {
        Sequence disableAnimation = DOTween.Sequence();

        for (int i = 0; i < scrollView!.GetScrollRange(); i++) {
            var button = (SlopCrewButton) scrollView.GetButtonByRelativeIndex(i);
            RectTransform buttonRect = button.RectTransform();
            if (i != scrollView.GetSelectorPos()) {
                button.ToggleBackground(false);
                disableAnimation.Append(buttonRect.DOAnchorPosX(-MyPhone.ScreenSize.x, 0.1f));
            }
        }
        disableAnimation.AppendCallback(() => OpenApp(selectedCategory));
    }

    private void OpenApp(Category app) {
        switch (app) {
            case Category.Chat:
                MyPhone.OpenApp(typeof(AppQuickChat));
                break;
            case Category.Encounters:
                MyPhone.OpenApp(typeof(AppEncounters));
                break;
        }
    }
}
