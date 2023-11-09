using BepInEx.Logging;
using Reptile.Phone;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlopCrew.Plugin.UI.Phone;

internal class SlopCrewButton : PhoneScrollButton {
    // Fields need to be serialized if Instantiate() should copy them
    [SerializeField]
    private Image? buttonBackground;
    [SerializeField]
    private Image? buttonIcon;
    [SerializeField]
    private TextMeshProUGUI? modeLabel;
    [SerializeField]
    private GameObject? confirmArrow;

    [SerializeField]
    private Sprite? normalButtonSprite;
    [SerializeField]
    private Sprite? selectedButtonSprite;

    [SerializeField]
    private Color normalModeColor = Color.white;
    [SerializeField]
    private Color selectedModeColor = new Color(0.196f, 0.305f, 0.612f);

    public void InitializeButton(Image buttonBackground,
                                 Image buttonIcon,
                                 TextMeshProUGUI modeLabel,
                                 GameObject confirmArrow,
                                 Sprite normalButtonSprite,
                                 Sprite selectedButtonSprite) {
        this.buttonBackground = buttonBackground;
        this.buttonIcon = buttonIcon;
        this.modeLabel = modeLabel;
        this.confirmArrow = confirmArrow;
        this.normalButtonSprite = normalButtonSprite;
        this.selectedButtonSprite = selectedButtonSprite;
    }

    public void SetButtonContents(string title, Sprite icon) {
        modeLabel.SetText(title);
        buttonIcon.sprite = icon;
    }

    protected override void OnSelect(bool skipAnimations = false) {
        base.OnSelect(skipAnimations);
        buttonBackground.sprite = selectedButtonSprite;
        modeLabel.color = selectedModeColor;
        confirmArrow.SetActive(true);
    }

    protected override void OnDeselect(bool skipAnimations = false) {
        base.OnDeselect(skipAnimations);
        buttonBackground.sprite = normalButtonSprite;
        modeLabel.color = normalModeColor;
        confirmArrow.SetActive(false);
    }
}
