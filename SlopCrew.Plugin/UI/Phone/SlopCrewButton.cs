using Reptile.Phone;
using SlopCrew.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlopCrew.Plugin.UI.Phone;

internal class SlopCrewButton : PhoneScrollButton {
    private AppSlopCrew? app;
    private EncounterType encounterType;

    // Fields need to be serialized if Instantiate() should copy them
    [SerializeField]
    private Image? buttonBackground;
    [SerializeField]
    private Image? buttonIcon;
    [SerializeField]
    private TextMeshProUGUI? modeLabel;
    [SerializeField]
    private TextMeshProUGUI? descriptionLabel;
    [SerializeField]
    private TextMeshProUGUI? statusLabel;
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

    [SerializeField]
    private CanvasGroup? canvasGroup;

    public bool Unavailable { get; private set; }
    public AppSlopCrew.EncounterStatus Status { get; private set; }

    public void InitializeButton(CanvasGroup canvasGroup,
                                 Image buttonBackground,
                                 Image buttonIcon,
                                 TextMeshProUGUI modeLabel,
                                 TextMeshProUGUI descriptionLabel,
                                 TextMeshProUGUI statusLabel,
                                 GameObject confirmArrow,
                                 Sprite normalButtonSprite,
                                 Sprite selectedButtonSprite) {
        this.canvasGroup = canvasGroup;
        this.buttonBackground = buttonBackground;
        this.buttonIcon = buttonIcon;
        this.modeLabel = modeLabel;
        this.descriptionLabel = descriptionLabel;
        this.statusLabel = statusLabel;
        this.confirmArrow = confirmArrow;
        this.normalButtonSprite = normalButtonSprite;
        this.selectedButtonSprite = selectedButtonSprite;
    }

    public void SetApp(AppSlopCrew app) {
        this.app = app;
    }

    public void SetButtonContents(EncounterType type, Sprite icon) {
        string title = "Encounter";
        string description = "Description";

        switch (type) {
            case EncounterType.ScoreEncounter:
                title = "Score Battle";
                description = "Short battle";
                break;
            case EncounterType.ComboEncounter:
                title = "Combo Battle";
                description = "Long battle";
                break;
            case EncounterType.RaceEncounter:
                title = "Race";
                description = string.Empty;
                break;
        }

        modeLabel.SetText(title);
        descriptionLabel.SetText(description);
        buttonIcon.sprite = icon;

        this.encounterType = type;
    }

    protected override void OnSelect(bool skipAnimations = false) {
        base.OnSelect(skipAnimations);
        buttonBackground.sprite = selectedButtonSprite;
        modeLabel.color = selectedModeColor;
        descriptionLabel.color = selectedModeColor;
        statusLabel.color = selectedModeColor;
        confirmArrow.SetActive(true);
    }

    protected override void OnDeselect(bool skipAnimations = false) {
        base.OnDeselect(skipAnimations);
        buttonBackground.sprite = normalButtonSprite;
        modeLabel.color = normalModeColor;
        descriptionLabel.color = normalModeColor;
        statusLabel.color = normalModeColor;
        confirmArrow.SetActive(false);
    }

    protected override void ConstantUpdate() {
        if (Plugin.CurrentEncounter?.IsBusy == true) {
            SetUnavailable(!app.IsActiveEncounter(this.encounterType));
            return;
        } else {
            SetUnavailable(false);
        }

        switch (encounterType) {
            case EncounterType.ScoreEncounter:
            case EncounterType.ComboEncounter:
                SetUnavailable(!app.HasNearbyPlayer, "No player nearby");
                break;
            case EncounterType.RaceEncounter:
                break;
        }
    }

    public void SetStatus(AppSlopCrew.EncounterStatus status) {
        Status = status;

        if (Unavailable) {
            return;
        }

        statusLabel.gameObject.SetActive(status != AppSlopCrew.EncounterStatus.None);

        switch (status) {
            case AppSlopCrew.EncounterStatus.WaitingStart:
                statusLabel.SetText("Waiting...");
                break;
            case AppSlopCrew.EncounterStatus.InProgress:
                statusLabel.SetText("In progress!");
                break;
            case AppSlopCrew.EncounterStatus.WaitingResults:
                statusLabel.SetText("Awaiting results...");
                break;
        }
    }

    private void SetUnavailable(bool value, string message = "") {
        if (Unavailable == value) {
            return;
        }

        Unavailable = value;

        if (value) {
            if (message != string.Empty) {
                statusLabel.gameObject.SetActive(true);
                statusLabel.SetText($"<color=red>{message}");
            }
            canvasGroup.alpha = 0.5f;
        } else {
            canvasGroup.alpha = 1.0f;
            // Need to update the text based on the status again because we overwrote it
            SetStatus(this.Status);
        }
    }
}
