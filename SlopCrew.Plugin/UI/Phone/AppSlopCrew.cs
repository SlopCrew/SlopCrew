using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using Reptile;
using Reptile.Phone;
using SlopCrew.Common;
using SlopCrew.Common.Network.Serverbound;
using SlopCrew.Plugin.Encounters;
using SlopCrew.Plugin.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlopCrew.Plugin.UI.Phone;

public class AppSlopCrew : App {
    public enum EncounterStatus {
        None,
        Waiting,
        InProgress,
        WaitingResults,
    }

    private SlopCrewScrollView? scrollView;
    private TextMeshProUGUI? statusTitle;
    private TextMeshProUGUI? statusMessage;

    private AssociatedPlayer? nearestPlayer;
    private EncounterType currentEncounter;
    private bool isWaitingForEncounter = false;
    private bool isDisplayingForcedText = false;

    private EncounterType[] encounterTypes = (EncounterType[]) Enum.GetValues(typeof(EncounterType));

    private bool notifInitialized;
    private bool playerLocked;
    private bool hasBannedMods;
    private string titleText = string.Empty;
    private string messageText = string.Empty;

    private ManualLogSource log = BepInEx.Logging.Logger.CreateLogSource("Slop Crew App");

    public override void Awake() {
        this.m_Unlockables = Array.Empty<AUnlockable>();
        base.Awake();
    }

    protected override void OnAppInit() {
        var contentObject = new GameObject("Content");
        contentObject.layer = Layers.Phone;
        contentObject.transform.SetParent(transform, false);
        contentObject.transform.localScale = Vector3.one;

        var content = contentObject.AddComponent<RectTransform>();
        content.sizeDelta = new(1070, 1775);

        var musicApp = this.MyPhone.GetAppInstance<AppMusicPlayer>();
        AddScrollView(musicApp, content);
        AddOverlay(musicApp, content);

        NearestPlayerChanged(null);
    }

    private void AddOverlay(AppMusicPlayer musicApp, RectTransform content) {
        // Overlay
        var overlay = musicApp.transform.Find("Content/Overlay").gameObject;
        GameObject slopCrewOverlay = Instantiate(overlay, content);

        var title = slopCrewOverlay.transform.Find("Icons/HeaderLabel").GetComponent<TextMeshProUGUI>();
        Destroy(title.GetComponent<TMProLocalizationAddOn>());
        title.SetText("Slop Crew");

        var overlayHeaderImage = slopCrewOverlay.transform.Find("OverlayTop");
        overlayHeaderImage.localPosition = Vector2.up * 870.0f;
        var overlayBottomImage = slopCrewOverlay.transform.Find("OverlayBottom");
        overlayBottomImage.localPosition = Vector2.down * 870.0f;

        var iconImage = slopCrewOverlay.transform.Find("Icons/AppIcon").GetComponent<Image>();
        iconImage.sprite = TextureLoader.LoadResourceAsSprite("SlopCrew.Plugin.res.phone_icon.png", 128, 128);

        // Status panel
        var status = musicApp.transform.Find("Content/StatusPanel").gameObject;
        GameObject slopCrewStatusPanel = Instantiate(status, content);

        // Get rid of music player UI stuff we don't need
        Destroy(slopCrewStatusPanel.GetComponent<MusicPlayerStatusPanel>());
        Destroy(slopCrewStatusPanel.transform.Find("Progress").gameObject);
        Destroy(slopCrewStatusPanel.transform.Find("ShuffleControl").gameObject);
        Destroy(slopCrewStatusPanel.transform.Find("LeftSide").gameObject);

        var textContainer = slopCrewStatusPanel.transform.Find("RightSide");
        textContainer.localPosition = new Vector2(-480.0f, 150.7f);

        statusTitle = textContainer.Find("CurrentTitleLabel").GetComponent<TextMeshProUGUI>();
        statusMessage = textContainer.Find("CurrentArtistLabel").GetComponent<TextMeshProUGUI>();
        statusMessage.transform.localPosition = new Vector2(0.0f, 64.0f);
        statusMessage.name = "CurrentStatusLabel";

        statusTitle.rectTransform.sizeDelta = statusMessage.rectTransform.sizeDelta = new Vector2(1000.0f, 128.0f);

        statusTitle.SetText("NEAREST PLAYER");
    }

    private void AddScrollView(AppMusicPlayer musicApp, RectTransform content) {
        // I really just do not want to hack together custom objects for sprites the game already loads anyway
        var musicTraverse = Traverse.Create(musicApp);
        var musicList = musicTraverse.Field("m_TrackList").GetValue() as MusicPlayerTrackList;
        var musicListTraverse = Traverse.Create(musicList);
        var musicButtonPrefab = musicListTraverse.Field("m_AppButtonPrefab").GetValue() as GameObject;

        var confirmArrow = musicButtonPrefab.transform.Find("PromptArrow");
        var titleLabel = musicButtonPrefab.transform.Find("TitleLabel").GetComponent<TextMeshProUGUI>();

        var scrollViewObject = new GameObject("Buttons");
        scrollViewObject.layer = Layers.Phone;
        var scrollViewRect = scrollViewObject.AddComponent<RectTransform>();
        scrollViewRect.SetParent(content, false);
        scrollView = scrollViewObject.AddComponent<SlopCrewScrollView>();
        scrollView.Initialize(this, confirmArrow.gameObject, titleLabel);
        scrollView.InitalizeScrollView();
    }

    public override void OnAppEnable() {
        base.OnAppEnable();

        scrollView.SetListContent(encounterTypes.Length);

        // I don't think anyone's going to just disable or enable banned mods while the game is running?
        // So we can probably cache the result when opening the app instead of asking every frame
        this.hasBannedMods = HasBannedMods();
        if (this.hasBannedMods) {
            scrollView.CanvasGroup.alpha = 0.5f;
            SetStatusText("Please disable adavantageous mods.", string.Empty);
            foreach (var button in scrollView.GetButtons()) {
                button.IsSelected = false;
            }
        }
    }

    public override void OnPressUp() {
        if (this.hasBannedMods) {
            return;
        }

        DismissForcedText();
        scrollView.Move(PhoneScroll.ScrollDirection.Previous, m_AudioManager);
    }

    public override void OnPressDown() {
        if (this.hasBannedMods) {
            return;
        }

        DismissForcedText();
        scrollView.Move(PhoneScroll.ScrollDirection.Next, m_AudioManager);
    }

    public override void OnPressRight() {
        if (this.hasBannedMods) {
            return;
        }

        EncounterType currentSelectedMode = (EncounterType) scrollView.GetContentIndex();

        if (ModeRequiresNearbyPlayer(currentSelectedMode) && this.nearestPlayer == null) {
            return;
        }

        if (isWaitingForEncounter && currentSelectedMode != currentEncounter) {
            return;
        }

        scrollView.HoldAnimationSelectedButton();
    }

    public override void OnReleaseRight() {
        if (this.hasBannedMods) {
            return;
        }

        EncounterType currentSelectedMode = (EncounterType) scrollView.GetContentIndex();

        if (ModeRequiresNearbyPlayer(currentSelectedMode) && this.nearestPlayer == null) {
            return;
        }

        if (isWaitingForEncounter && currentSelectedMode == currentEncounter) {
            SendCancelEncounterRequest(currentEncounter);
            return;
        }

        if (!SendEncounterRequest(currentSelectedMode)) return;

        scrollView.ActivateAnimationSelectedButton();
        m_AudioManager.PlaySfx(SfxCollectionID.PhoneSfx, AudioClipID.FlipPhone_Confirm);

        if (currentSelectedMode == EncounterType.RaceEncounter && !isWaitingForEncounter) {
            currentEncounter = EncounterType.RaceEncounter;
            isWaitingForEncounter = true;
            SetEncounterStatus(EncounterStatus.Waiting);
        }
    }

    private bool SendEncounterRequest(EncounterType encounter) {
        log.LogMessage($"Sent encounter request for {encounter}");
        return true;

        if (!encounter.IsStateful() && this.nearestPlayer == null) return false;
        if (Plugin.CurrentEncounter?.IsBusy == true) return false;
        if (hasBannedMods) return false;

        Plugin.HasEncounterBeenCancelled = false;

        Plugin.NetworkConnection.SendMessage(new ServerboundEncounterRequest {
            PlayerID = this.nearestPlayer?.SlopPlayer.ID ?? uint.MaxValue,
            EncounterType = encounter
        });

        return true;
    }

    private void SendCancelEncounterRequest(EncounterType encounter) {
        log.LogMessage($"Sent cancel encounter request for {encounter}");
        EndWaitingForEncounter();
        return;

        Plugin.NetworkConnection.SendMessage(new ServerboundEncounterCancel {
            EncounterType = encounter
        });
    }

    public override void OnAppUpdate() {
        var player = WorldHandler.instance.GetCurrentPlayer();
        if (player is null) return;

        if (Plugin.CurrentEncounter is SlopRaceEncounter && isWaitingForEncounter && currentEncounter == EncounterType.RaceEncounter) {
            EndWaitingForEncounter();
        }

        if (isWaitingForEncounter && currentEncounter == EncounterType.RaceEncounter) {
            if (Plugin.HasEncounterBeenCancelled) {
                EndWaitingForEncounter();
            }
            return;
        }

        if (hasBannedMods) {
            return;
        }

        if (Plugin.CurrentEncounter?.IsBusy == true) {
            if (Plugin.CurrentEncounter is SlopRaceEncounter race && race.IsWaitingForResults()) {
                SetEncounterStatus(EncounterStatus.WaitingResults);
            } else {
                SetEncounterStatus(EncounterStatus.InProgress);
            }
            return;
        }

        if (!this.playerLocked) {
            var position = player.transform.position;
            var nearestPlayer = Plugin.PlayerManager.AssociatedPlayers
                .Where(x => x.IsValid())
                .OrderBy(x => Vector3.Distance(x.ReptilePlayer.transform.position, position))
                .FirstOrDefault();

            if (nearestPlayer != this.nearestPlayer) {
                NearestPlayerChanged(nearestPlayer);
            }
        }
    }

    private bool HasBannedMods() {
        var bannedMods = Plugin.NetworkConnection.ServerConfig?.BannedMods ?? new();
        return Chainloader.PluginInfos.Keys.Any(x => bannedMods.Contains(x));
    }

    public void EndWaitingForEncounter() {
        isWaitingForEncounter = false;
        SetEncounterStatus(EncounterStatus.None);
    }

    private bool ModeRequiresNearbyPlayer(EncounterType mode) {
        if (mode == EncounterType.RaceEncounter) {
            return false;
        }

        return true;
    }

    private void SetStatusText(string title, string message) {
        if (isDisplayingForcedText) {
            titleText = title;
            messageText = message;
            return;
        }

        if (statusTitle.text != title) {
            statusTitle.SetText(title);
        }
        if (statusMessage.text != message) {
            statusMessage.SetText(message);
        }
    }

    public void SetForcedText(string title, string result) {
        SetStatusText(title, result);
        isDisplayingForcedText = true;
    }

    private void DismissForcedText() {
        if (!isDisplayingForcedText) {
            return;
        }

        isDisplayingForcedText = false;
        SetStatusText(titleText, messageText);
    }

    private void SetEncounterStatus(EncounterStatus status) {
        var button = scrollView.GetButtonByRelativeIndex((int) currentEncounter) as SlopCrewButton;
        if (button == null) return;

        if (button.Status == status) {
            return;
        }

        button.SetStatus(status);
    }

    private void NearestPlayerChanged(AssociatedPlayer? player) {
        if (player == null) {
            if (this.playerLocked) this.playerLocked = false;
            SetStatusText("NEAREST PLAYER", "None found.");
            return;
        }

        string playerName = PlayerNameFilter.DoFilter(player.SlopPlayer.Name);

        if (this.playerLocked) {
            SetStatusText("NEAREST PLAYER", $"<b>{playerName}");
        } else {
            SetStatusText("NEAREST PLAYER", playerName);
        }
    }

    public void SetNotification(Notification notif) {
        if (this.notifInitialized) return;
        var newNotif = Instantiate(notif.gameObject, this.transform);
        this.m_Notification = newNotif.GetComponent<Notification>();
        this.m_Notification.InitNotification(this);
        this.notifInitialized = true;
    }

    public override void OpenContent(AUnlockable unlockable, bool appAlreadyOpen) {
        if (Plugin.PhoneInitializer.LastRequest is not null) {
            var request = Plugin.PhoneInitializer.LastRequest;

            if (Plugin.PlayerManager.Players.TryGetValue(request.PlayerID, out var player)) {
                this.nearestPlayer = player;
                if (Plugin.SlopConfig.StartEncountersOnRequest.Value) {
                    this.SendEncounterRequest(request.EncounterType);
                } else {
                    this.playerLocked = true;
                    Task.Run(() => {
                        Task.Delay(5000).Wait();
                        this.playerLocked = false;
                    });
                }
            }
        }

        Plugin.PhoneInitializer.LastRequest = null;
    }
}
