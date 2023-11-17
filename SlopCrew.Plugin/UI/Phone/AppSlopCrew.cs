using BepInEx.Bootstrap;
using BepInEx.Logging;
using Reptile;
using Reptile.Phone;
using SlopCrew.Common;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SlopCrew.Common.Proto;
using SlopCrew.Plugin.Encounters;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using RaceEncounter = SlopCrew.Plugin.Encounters.RaceEncounter;
using Vector3 = UnityEngine.Vector3;
using DG.Tweening;

namespace SlopCrew.Plugin.UI.Phone;

public class AppSlopCrew : App {
    public enum EncounterStatus {
        None,
        WaitingStart,
        InProgress,
        WaitingResults,
    }

    public static readonly int EncounterCount = Enum.GetValues(typeof(EncounterType)).Length;
    public static ClientboundEncounterRequest? LastRequest;

    public bool HasNearbyPlayer => nearestPlayer != null;

    private SlopCrewScrollView? scrollView;
    private TextMeshProUGUI? statusTitle;
    private TextMeshProUGUI? statusMessage;

    private AssociatedPlayer? nearestPlayer;
    private EncounterType currentEncounter;
    public bool isWaitingForEncounter = false;
    private bool isDisplayingForcedText = false;

    private bool notifInitialized;
    private bool playerLocked;
    private bool hasBannedMods;
    private string titleText = string.Empty;
    private string messageText = string.Empty;
    private float messageInitialX;
    private Sequence messageTextSequence;

    private EncounterManager encounterManager = null!;
    private ConnectionManager connectionManager = null!;
    private PlayerManager playerManager = null!;
    private Config config = null!;
    private ServerConfig serverConfig = null!;

    public override void Awake() {
        this.m_Unlockables = Array.Empty<AUnlockable>();

        this.encounterManager = Plugin.Host.Services.GetRequiredService<EncounterManager>();
        this.connectionManager = Plugin.Host.Services.GetRequiredService<ConnectionManager>();
        this.playerManager = Plugin.Host.Services.GetRequiredService<PlayerManager>();
        this.config = Plugin.Host.Services.GetRequiredService<Config>();
        this.serverConfig = Plugin.Host.Services.GetRequiredService<ServerConfig>();

        base.Awake();
    }

    public override void OnAppInit() {
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
        iconImage.sprite = TextureLoader.LoadResourceAsSprite("SlopCrew.Plugin.res.phone_icon.png", 256, 256);

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

        messageInitialX = statusMessage.rectTransform.anchoredPosition.x;
        statusMessage.rectTransform.anchoredPosition = statusMessage.rectTransform.anchoredPosition + Vector2.right * 256.0f;
        statusMessage.alpha = 0.0f;

        messageTextSequence = DOTween.Sequence();
        messageTextSequence.SetAutoKill(false);
        messageTextSequence.Append(statusMessage.rectTransform.DOAnchorPosX(messageInitialX, 0.2f));
        messageTextSequence.Join(statusMessage.DOFade(1.0f, 0.2f));

        statusTitle.rectTransform.sizeDelta = statusMessage.rectTransform.sizeDelta = new Vector2(1000.0f, 128.0f);

        statusTitle.SetText("NEAREST PLAYER");
    }

    private void AddScrollView(AppMusicPlayer musicApp, RectTransform content) {
        // I really just do not want to hack together custom objects for sprites the game already loads anyway
        var musicButtonPrefab = musicApp.m_TrackList.m_AppButtonPrefab;

        var confirmArrow = musicButtonPrefab.transform.Find("PromptArrow");
        var titleLabel = musicButtonPrefab.transform.Find("TitleLabel").GetComponent<TextMeshProUGUI>();

        var scrollViewObject = new GameObject("Buttons");
        scrollViewObject.layer = Layers.Phone;
        var scrollViewRect = scrollViewObject.AddComponent<RectTransform>();
        scrollViewRect.SetParent(content, false);
        scrollView = scrollViewObject.AddComponent<SlopCrewScrollView>();
        scrollView.Initialize(this, confirmArrow.gameObject, titleLabel);
        scrollView.InitalizeScrollView();
        scrollView.SetListContent(EncounterCount);
    }

    public override void OnAppEnable() {
        base.OnAppEnable();

        // I don't think anyone's going to just disable or enable banned mods while the game is running?
        // So we can probably cache the result when opening the app instead of asking every frame
        this.hasBannedMods = HasBannedMods();
        if (this.hasBannedMods) {
            scrollView.CanvasGroup.alpha = 0.5f;
            SetStatusText("Please disable adavantageous mods.", string.Empty);
            foreach (var button in scrollView.GetButtons()) {
                button.IsSelected = false;
            }
            scrollView.enabled = false;
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

        int contentIndex = scrollView.GetContentIndex();
        var currentSelectedMode = (EncounterType) contentIndex;

        var selectedButton = scrollView.GetButtonByRelativeIndex(contentIndex) as SlopCrewButton;
        if (selectedButton.Unavailable) {
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

        scrollView.ActivateAnimationSelectedButton();

        var contentIndex = scrollView.GetContentIndex();
        var currentSelectedMode = (EncounterType) contentIndex;

        var selectedButton = scrollView.GetButtonByRelativeIndex(contentIndex) as SlopCrewButton;
        if (selectedButton.Unavailable) {
            return;
        }

        if (isWaitingForEncounter && currentSelectedMode == currentEncounter) {
            SendCancelEncounterRequest(currentEncounter);
            return;
        }

        if (!SendEncounterRequest(currentSelectedMode)) return;

        m_AudioManager.PlaySfxUI(SfxCollectionID.PhoneSfx, AudioClipID.FlipPhone_Confirm);

        if (currentSelectedMode == EncounterType.Race && !isWaitingForEncounter) {
            currentEncounter = EncounterType.Race;
            isWaitingForEncounter = true;
            SetEncounterStatus(EncounterStatus.WaitingStart);
        }
    }

    private bool SendEncounterRequest(EncounterType encounter) {
        // TODO: races
        if (encounter is not EncounterType.Race && this.nearestPlayer == null) return false;
        if (this.encounterManager.CurrentEncounter?.IsBusy == true) return false;
        if (hasBannedMods) return false;

        var id = this.nearestPlayer?.SlopPlayer.Id;

        var encounterRequest = new ServerboundEncounterRequest {
            Type = encounter
        };
        if (id is not null) encounterRequest.PlayerId = id.Value;

        this.connectionManager.SendMessage(new ServerboundMessage {
            EncounterRequest = encounterRequest
        });

        currentEncounter = encounter;
        return true;
    }

    private void SendCancelEncounterRequest(EncounterType encounter) {
        // TODO
        /*Plugin.NetworkConnection.SendMessage(new ServerboundEncounterCancel {
            EncounterType = encounter
        });*/
    }

    public override void OnAppUpdate() {
        var player = WorldHandler.instance.GetCurrentPlayer();
        if (player is null) return;

        if (this.encounterManager.CurrentEncounter is RaceEncounter
            && this.isWaitingForEncounter
            && this.currentEncounter == EncounterType.Race) {
            this.EndWaitingForEncounter();
        }

        if (this.hasBannedMods) return;

        if (this.encounterManager.CurrentEncounter?.IsBusy == true) {
            if (this.encounterManager.CurrentEncounter is RaceEncounter race && race.IsWaitingForResults()) {
                SetEncounterStatus(EncounterStatus.WaitingResults);
            } else {
                SetEncounterStatus(EncounterStatus.InProgress);
            }
            return;
        }

        // This happens when literally no encounter is going on or one just ended
        if (!this.isWaitingForEncounter) this.SetEncounterStatus(EncounterStatus.None);

        if (!this.playerLocked) {
            var position = player.transform.position;
            var newNearestPlayer = this.playerManager.AssociatedPlayers
                .Where(x => x.ReptilePlayer != null)
                .OrderBy(x => Vector3.Distance(x.ReptilePlayer.transform.position, position))
                .FirstOrDefault();

            if (newNearestPlayer != this.nearestPlayer) {
                this.NearestPlayerChanged(newNearestPlayer);
            }
        }
    }

    private void SetNotification(Notification notif) {
        if (this.notifInitialized) return;
        var newNotif = Instantiate(notif.gameObject, this.transform);
        this.m_Notification = newNotif.GetComponent<Notification>();
        this.m_Notification.InitNotification(this);
        this.notifInitialized = true;
    }

    public override void OpenContent(AUnlockable unlockable, bool appAlreadyOpen) {
        if (LastRequest is not null) {
            if (this.playerManager.Players.TryGetValue(LastRequest.PlayerId, out var player)) {
                this.nearestPlayer = player;

                if (this.config.Phone.StartEncountersOnRequest.Value) {
                    this.SendEncounterRequest(LastRequest.Type);
                } else {
                    this.playerLocked = true;
                    Task.Run(() => {
                        Task.Delay(5000).Wait();
                        this.playerLocked = false;
                    });
                }
            }
        }

        LastRequest = null;
    }

    private bool HasBannedMods() {
        var bannedMods = this.serverConfig.Hello?.BannedPlugins ?? new();
        return Chainloader.PluginInfos.Keys.Any(x => bannedMods.Contains(x));
    }

    public void EndWaitingForEncounter() {
        isWaitingForEncounter = false;
        SetEncounterStatus(EncounterStatus.None);
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
        this.nearestPlayer = player;

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

        messageTextSequence.Restart();
    }

    public bool IsActiveEncounter(EncounterType type) {
        return currentEncounter == type;
    }

    public void ProcessEncounterRequest(ClientboundEncounterRequest request) {
        if (!this.config.Phone.ReceiveNotifications.Value) return;
        if (this.encounterManager.CurrentEncounter?.IsBusy == true) return;

        if (this.playerManager.Players.TryGetValue(request.PlayerId, out var associatedPlayer)) {
            var me = WorldHandler.instance.GetCurrentPlayer();
            if (me == null) return;

            LastRequest = request;

            var phone = me.phone;
            var emailApp = phone.GetAppInstance<AppEmail>();
            var emailNotif = emailApp.GetComponent<Notification>();
            this.SetNotification(emailNotif);

            var name = PlayerNameFilter.DoFilter(associatedPlayer.SlopPlayer.Name);
            phone.PushNotification(this, name, null);
        }
    }
}
