using Reptile.Phone;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable ParameterHidesMember

namespace SlopCrew.Plugin.UI.Phone;

internal class SlopCrewButton : PhoneScrollButton {
    // Fields need to be serialized if Instantiate() should copy them
    [SerializeField] private Image? buttonBackground;
    [SerializeField] private Image? buttonIcon;
    [SerializeField] private TextMeshProUGUI? label;
    [SerializeField] private GameObject? confirmArrow;

    [SerializeField] private Sprite? normalButtonSprite;
    [SerializeField] private Sprite? selectedButtonSprite;

    [SerializeField] private Color normalModeColor = Color.white;
    [SerializeField] private Color selectedModeColor = new Color(0.196f, 0.305f, 0.612f);

    public void InitializeButton(
        Image buttonBackground,
        Image buttonIcon,
        TextMeshProUGUI modeLabel,
        GameObject confirmArrow,
        Sprite normalButtonSprite,
        Sprite selectedButtonSprite
    ) {
        this.buttonBackground = buttonBackground;
        this.buttonIcon = buttonIcon;
        this.label = modeLabel;
        this.confirmArrow = confirmArrow;
        this.normalButtonSprite = normalButtonSprite;
        this.selectedButtonSprite = selectedButtonSprite;
    }

    public void SetButtonContents(AppSlopCrew.Category category, Sprite icon) {
        var title = "Category";

        switch (category) {
            case AppSlopCrew.Category.Chat:
                title = "Quick Chat";
                break;
            case AppSlopCrew.Category.Encounters:
                title = "Activities";
                break;
        }

        this.label!.SetText(title);
        this.buttonIcon!.sprite = icon;
    }

    public override void OnSelect(bool skipAnimations = false) {
        base.OnSelect(skipAnimations);
        this.buttonBackground!.sprite = this.selectedButtonSprite;
        this.buttonIcon!.color = this.selectedModeColor;
        this.label!.color = this.selectedModeColor;
        this.confirmArrow!.SetActive(true);
    }

    public override void OnDeselect(bool skipAnimations = false) {
        base.OnDeselect(skipAnimations);
        this.buttonBackground!.enabled = true;
        this.buttonBackground!.sprite = this.normalButtonSprite;
        this.buttonIcon!.color = this.normalModeColor;
        this.label!.color = this.normalModeColor;
        this.confirmArrow!.SetActive(false);
    }

    public void ToggleBackground(bool visible) {
        this.buttonBackground!.enabled = visible;
    }
}
