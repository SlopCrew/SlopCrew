using Reptile;
using Reptile.Phone;
using System;
using SlopCrew.Common.Proto;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static SlopCrew.Plugin.UI.Phone.AppEncounters;

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
        AddOverlay(musicApp, Content);
    }

    private void AddOverlay(AppMusicPlayer musicApp, RectTransform root) {
        // Overlay
        var overlay = musicApp.transform.Find("Content/Overlay").gameObject;
        GameObject slopCrewOverlay = Instantiate(overlay, root);

        var title = slopCrewOverlay.transform.Find("Icons/HeaderLabel").GetComponent<TextMeshProUGUI>();
        Destroy(title.GetComponent<TMProLocalizationAddOn>());
        title.SetText("Slop Crew");

        var overlayHeaderImage = slopCrewOverlay.transform.Find("OverlayTop");
        overlayHeaderImage.localPosition = Vector2.up * 870.0f;
        var overlayBottomImage = slopCrewOverlay.transform.Find("OverlayBottom");
        Destroy(overlayBottomImage.gameObject);

        var iconImage = slopCrewOverlay.transform.Find("Icons/AppIcon").GetComponent<Image>();
        iconImage.sprite = TextureLoader.LoadResourceAsSprite("SlopCrew.Plugin.res.phone_icon.png", 256, 256);
    }

    public override void OnPressUp() {
        scrollView!.Move(PhoneScroll.ScrollDirection.Previous, m_AudioManager);
    }

    public override void OnPressDown() {
        scrollView!.Move(PhoneScroll.ScrollDirection.Next, m_AudioManager);
    }

    public override void OnPressRight() {
        int contentIndex = scrollView!.GetContentIndex();
        var selectedCategory = (Category) contentIndex;
        var button = scrollView!.GetButtonByRelativeIndex(contentIndex);

        scrollView!.HoldAnimationSelectedButton();
    }

    public override void OnReleaseRight() {
        int contentIndex = scrollView!.GetContentIndex();
        var selectedCategory = (Category) contentIndex;
        var button = scrollView!.GetButtonByRelativeIndex(contentIndex);

        scrollView!.ActivateAnimationSelectedButton();
    }
}
