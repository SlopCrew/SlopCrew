using System;
using Microsoft.Extensions.DependencyInjection;
using Reptile.Phone;
using SlopCrew.Common.Proto;
using SlopCrew.Plugin.Encounters;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable ParameterHidesMember

namespace SlopCrew.Plugin.UI.Phone;

internal class EncounterButton : PhoneScrollButton {
    private AppEncounters? app;
    private EncounterType encounterType;

    // Fields need to be serialized if Instantiate() should copy them
    [SerializeField] private Image? buttonBackground;
    [SerializeField] private Image? buttonIcon;
    [SerializeField] private TextMeshProUGUI? modeLabel;
    [SerializeField] private TextMeshProUGUI? descriptionLabel;
    [SerializeField] private TextMeshProUGUI? statusLabel;
    [SerializeField] private GameObject? confirmArrow;

    [SerializeField] private Sprite? normalButtonSprite;
    [SerializeField] private Sprite? selectedButtonSprite;

    [SerializeField] private Color normalModeColor = Color.white;
    [SerializeField] private Color selectedModeColor = new Color(0.196f, 0.305f, 0.612f);

    [SerializeField] private CanvasGroup? canvasGroup;

    public bool Unavailable { get; private set; }
    public AppEncounters.EncounterStatus Status { get; private set; }

    private EncounterManager? encounterManager;

    public void InitializeButton(
        CanvasGroup canvasGroup,
        Image buttonBackground,
        Image buttonIcon,
        TextMeshProUGUI modeLabel,
        TextMeshProUGUI descriptionLabel,
        TextMeshProUGUI statusLabel,
        GameObject confirmArrow,
        Sprite normalButtonSprite,
        Sprite selectedButtonSprite
    ) {
        this.canvasGroup = canvasGroup;
        this.buttonBackground = buttonBackground;
        this.buttonIcon = buttonIcon;
        this.modeLabel = modeLabel;
        this.descriptionLabel = descriptionLabel;
        this.statusLabel = statusLabel;
        this.confirmArrow = confirmArrow;
        this.normalButtonSprite = normalButtonSprite;
        this.selectedButtonSprite = selectedButtonSprite;

        this.encounterManager = Plugin.Host.Services.GetRequiredService<EncounterManager>();
    }

    public void SetApp(AppEncounters app) {
        this.app = app;
    }

    public void SetButtonContents(EncounterType type, Sprite icon) {
        var title = "Encounter";
        var description = "Description";

        switch (type) {
            case EncounterType.ScoreBattle:
                title = "Score Battle";
                description = "Short battle";
                break;
            case EncounterType.ComboBattle:
                title = "Combo Battle";
                description = "Long battle";
                break;
            case EncounterType.Race:
                title = "Race";
                description = string.Empty;
                break;
        }

        this.modeLabel!.SetText(title);
        this.descriptionLabel!.SetText(description);
        this.buttonIcon!.sprite = icon;

        this.encounterType = type;
    }

    public override void OnSelect(bool skipAnimations = false) {
        base.OnSelect(skipAnimations);
        this.buttonBackground!.sprite = this.selectedButtonSprite;
        this.modeLabel!.color = this.selectedModeColor;
        this.descriptionLabel!.color = this.selectedModeColor;
        this.statusLabel!.color = this.normalModeColor;
        this.confirmArrow!.SetActive(true);
    }

    public override void OnDeselect(bool skipAnimations = false) {
        base.OnDeselect(skipAnimations);
        this.buttonBackground!.sprite = this.normalButtonSprite;
        this.modeLabel!.color = this.normalModeColor;
        this.descriptionLabel!.color = this.normalModeColor;
        this.statusLabel!.color = this.selectedModeColor;
        this.confirmArrow!.SetActive(false);
    }

    public override void ConstantUpdate() {
        if (this.encounterManager?.CurrentEncounter?.IsBusy == true) {
            this.SetUnavailable(this.app!.ActiveEncounter != this.encounterType);
            return;
        }

        switch (this.encounterType) {
            case EncounterType.ScoreBattle:
            case EncounterType.ComboBattle:
                this.SetUnavailable(!this.app!.HasNearbyPlayer, "No player nearby");
                break;
            case EncounterType.Race:
                this.SetUnavailable(false);
                break;
        }
    }

    public void SetStatus(AppEncounters.EncounterStatus status) {
        this.Status = status;
        if (this.Unavailable)
            return;

        this.statusLabel!.gameObject.SetActive(status != AppEncounters.EncounterStatus.None);
        switch (status) {
            case AppEncounters.EncounterStatus.WaitingStart:
                this.statusLabel.SetText("Waiting...");
                break;
            case AppEncounters.EncounterStatus.InProgress:
                this.statusLabel.SetText("In progress!");
                break;
            case AppEncounters.EncounterStatus.WaitingResults:
                this.statusLabel.SetText("Awaiting results...");
                break;
        }
    }

    private void SetUnavailable(bool value, string message = "") {
        if (this.Unavailable == value)
            return;
        this.Unavailable = value;

        if (value) {
            if (message != string.Empty) {
                this.statusLabel!.gameObject.SetActive(true);
                this.statusLabel.SetText($"<color=red>{message}");
            }

            this.canvasGroup!.alpha = 0.5f;
        } else {
            this.canvasGroup!.alpha = 1.0f;
            // Need to update the text based on the status again because we overwrote it
            this.SetStatus(this.Status);
        }
    }
}
