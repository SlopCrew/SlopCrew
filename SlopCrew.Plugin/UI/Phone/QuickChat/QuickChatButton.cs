using Reptile.Phone;
using SlopCrew.Common;
using SlopCrew.Common.Proto;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable ParameterHidesMember

namespace SlopCrew.Plugin.UI.Phone;

internal class QuickChatButton : PhoneScrollButton {
    // Fields need to be serialized if Instantiate() should copy them
    [SerializeField] private Image? buttonBackground;
    [SerializeField] private TextMeshProUGUI? label;
    [SerializeField] private GameObject? confirmArrow;

    [SerializeField] private Sprite? normalButtonSprite;
    [SerializeField] private Sprite? selectedButtonSprite;

    [SerializeField] private Color normalModeColor = Color.white;
    [SerializeField] private Color selectedModeColor = new Color(0.196f, 0.305f, 0.612f);

    private QuickChatCategory messageCategory;
    private int messageIndex;

    public void InitializeButton(
        Image buttonBackground,
        TextMeshProUGUI modeLabel,
        GameObject confirmArrow,
        Sprite normalButtonSprite,
        Sprite selectedButtonSprite
    ) {
        this.buttonBackground = buttonBackground;
        this.label = modeLabel;
        this.confirmArrow = confirmArrow;
        this.normalButtonSprite = normalButtonSprite;
        this.selectedButtonSprite = selectedButtonSprite;
    }

    public void SetButtonContents(QuickChatCategory category, int index) {
        this.messageCategory = category;
        this.messageIndex = index;
        this.label!.SetText(Constants.QuickChatMessages[category][index]);
    }

    public override void OnSelect(bool skipAnimations = false) {
        base.OnSelect(skipAnimations);
        this.buttonBackground!.sprite = this.selectedButtonSprite;
        this.label!.color = this.selectedModeColor;
        this.confirmArrow!.SetActive(true);
    }

    public override void OnDeselect(bool skipAnimations = false) {
        base.OnDeselect(skipAnimations);
        this.buttonBackground!.sprite = this.normalButtonSprite;
        this.label!.color = this.normalModeColor;
        this.confirmArrow!.SetActive(false);
    }

    public void GetMessage(out QuickChatCategory category, out int index) {
        category = this.messageCategory;
        index = this.messageIndex;
    }
}
