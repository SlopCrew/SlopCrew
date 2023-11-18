using BepInEx.Bootstrap;
using DG.Tweening;
using Microsoft.Extensions.DependencyInjection;
using Reptile;
using Reptile.Phone;
using SlopCrew.Common;
using SlopCrew.Common.Proto;
using SlopCrew.Plugin.Encounters;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using RaceEncounter = SlopCrew.Plugin.Encounters.RaceEncounter;
using Vector3 = UnityEngine.Vector3;

namespace SlopCrew.Plugin.UI.Phone;

public class AppEncounters : App {
    public enum EncounterStatus {
        None,
        WaitingStart,
        InProgress,
        WaitingResults,
    }

    public EncounterType ActiveEncounter { get; private set; }
    public bool HasNearbyPlayer => nearestPlayer != null;

    public static ClientboundEncounterRequest? PreviousEncounterRequest;

    private EncounterView? scrollView;

    private AssociatedPlayer? nearestPlayer;
    private bool isWaitingForEncounter = false;

    private bool opponentLocked;
    private bool hasBannedMods;
    private bool notificationInitialized;

    private TextMeshProUGUI? titleLabel;
    private TextMeshProUGUI? opponentNameLabel;
    private float initialOpponentNameX;
    private Sequence? nameDisplaySequence;

    private EncounterManager encounterManager = null!;
    private ConnectionManager connectionManager = null!;
    private PlayerManager playerManager = null!;
    private Config config = null!;
    private ServerConfig serverConfig = null!;

    public override void OnAppInit() {
        scrollView = ExtendedPhoneScroll.Create<EncounterView>("EncounterScrollView", this, Content);

        this.encounterManager = Plugin.Host.Services.GetRequiredService<EncounterManager>();
        this.connectionManager = Plugin.Host.Services.GetRequiredService<ConnectionManager>();
        this.playerManager = Plugin.Host.Services.GetRequiredService<PlayerManager>();
        this.config = Plugin.Host.Services.GetRequiredService<Config>();
        this.serverConfig = Plugin.Host.Services.GetRequiredService<ServerConfig>();

        AddOverlay();

        NearestPlayerChanged(null);
    }

    private void AddOverlay() {
        var musicApp = MyPhone.GetAppInstance<AppMusicPlayer>();

        // Overlay
        var overlay = musicApp.transform.Find("Content/Overlay").gameObject;
        GameObject slopCrewOverlay = Instantiate(overlay, Content);

        var icons = slopCrewOverlay.transform.Find("Icons");
        Destroy(icons.gameObject);

        var overlayHeaderImage = slopCrewOverlay.transform.Find("OverlayTop");
        Destroy(overlayHeaderImage.gameObject);
        var overlayBottomImage = slopCrewOverlay.transform.Find("OverlayBottom");
        overlayBottomImage.localPosition = Vector2.down * 870.0f;

        // Status panel
        var status = musicApp.transform.Find("Content/StatusPanel").gameObject;
        GameObject slopCrewStatusPanel = Instantiate(status, Content);

        // Get rid of music player UI stuff we don't need
        Destroy(slopCrewStatusPanel.GetComponent<MusicPlayerStatusPanel>());
        Destroy(slopCrewStatusPanel.transform.Find("Progress").gameObject);
        Destroy(slopCrewStatusPanel.transform.Find("ShuffleControl").gameObject);
        Destroy(slopCrewStatusPanel.transform.Find("LeftSide").gameObject);

        var textContainer = slopCrewStatusPanel.transform.Find("RightSide");
        textContainer.localPosition = new Vector2(-480.0f, 150.7f);

        titleLabel = textContainer.Find("CurrentTitleLabel").GetComponent<TextMeshProUGUI>();
        opponentNameLabel = textContainer.Find("CurrentArtistLabel").GetComponent<TextMeshProUGUI>();
        opponentNameLabel.transform.localPosition = new Vector2(0.0f, 64.0f);
        opponentNameLabel.name = "CurrentStatusLabel";

        initialOpponentNameX = opponentNameLabel.rectTransform.anchoredPosition.x;
        opponentNameLabel.rectTransform.anchoredPosition = opponentNameLabel.rectTransform.anchoredPosition + Vector2.right * 256.0f;
        opponentNameLabel.alpha = 0.0f;

        nameDisplaySequence = DOTween.Sequence();
        nameDisplaySequence.SetAutoKill(false);
        nameDisplaySequence.Append(opponentNameLabel.rectTransform.DOAnchorPosX(initialOpponentNameX, 0.2f));
        nameDisplaySequence.Join(opponentNameLabel.DOFade(1.0f, 0.2f));

        titleLabel.rectTransform.sizeDelta = opponentNameLabel.rectTransform.sizeDelta = new Vector2(1000.0f, 128.0f);
    }

    private void SetText(string title, string messsage) {
        if (titleLabel!.text != title) {
            titleLabel.SetText(title);
        }
        if (opponentNameLabel!.text != messsage) {
            opponentNameLabel.SetText(messsage);
        }
    }

    public void SetBigText(string title, string message) {
        // TODO
    }

    public override void OnAppEnable() {
        base.OnAppEnable();

        this.hasBannedMods = HasBannedMods();
        if (this.hasBannedMods) {
            scrollView!.CanvasGroup!.alpha = 0.5f;
            SetText("Please disable adavantageous mods.", string.Empty);
            foreach (var button in scrollView.GetButtons()) {
                button.IsSelected = false;
            }
            scrollView.enabled = false;

            return;
        }
    }

    public override void OnAppUpdate() {
        var player = WorldHandler.instance.GetCurrentPlayer();
        if (player is null) return;

        if (this.encounterManager.CurrentEncounter is RaceEncounter
            && this.isWaitingForEncounter
            && this.ActiveEncounter == EncounterType.Race) {
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

        if (!this.opponentLocked) {
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

    public override void OpenContent(AUnlockable unlockable, bool appAlreadyOpen) {
        if (PreviousEncounterRequest is not null) {
            if (this.playerManager.Players.TryGetValue(PreviousEncounterRequest.PlayerId, out var player)) {
                this.nearestPlayer = player;

                if (this.config.Phone.StartEncountersOnRequest.Value) {
                    this.SendEncounterRequest(PreviousEncounterRequest.Type);
                } else {
                    this.opponentLocked = true;
                    Task.Run(() => {
                        Task.Delay(5000).Wait();
                        this.opponentLocked = false;
                    });
                }
            }
        }

        PreviousEncounterRequest = null;
    }


    public override void OnPressUp() {
        if (this.hasBannedMods) {
            return;
        }

        scrollView!.Move(PhoneScroll.ScrollDirection.Previous, m_AudioManager);
    }

    public override void OnPressDown() {
        if (this.hasBannedMods) {
            return;
        }

        scrollView!.Move(PhoneScroll.ScrollDirection.Next, m_AudioManager);
    }

    public override void OnPressRight() {
        if (this.hasBannedMods) {
            return;
        }

        int contentIndex = scrollView!.GetContentIndex();
        var currentSelectedMode = (EncounterType) contentIndex;

        var selectedButton = scrollView!.GetButtonByRelativeIndex(contentIndex) as EncounterButton;
        if (selectedButton!.Unavailable) {
            return;
        }

        if (isWaitingForEncounter && currentSelectedMode != ActiveEncounter) {
            return;
        }

        scrollView!.HoldAnimationSelectedButton();
    }

    public override void OnReleaseRight() {
        if (this.hasBannedMods) {
            return;
        }

        scrollView!.ActivateAnimationSelectedButton();

        var contentIndex = scrollView!.GetContentIndex();
        var currentSelectedMode = (EncounterType) contentIndex;

        var selectedButton = scrollView!.GetButtonByRelativeIndex(contentIndex) as EncounterButton;
        if (selectedButton.Unavailable) {
            return;
        }

        if (isWaitingForEncounter && currentSelectedMode == ActiveEncounter) {
            SendCancelEncounterRequest(ActiveEncounter);
            return;
        }

        if (!SendEncounterRequest(currentSelectedMode)) return;

        m_AudioManager.PlaySfxUI(SfxCollectionID.PhoneSfx, AudioClipID.FlipPhone_Confirm);

        if (currentSelectedMode == EncounterType.Race && !isWaitingForEncounter) {
            ActiveEncounter = EncounterType.Race;
            isWaitingForEncounter = true;
            SetEncounterStatus(EncounterStatus.WaitingStart);
        }
    }

    private void NearestPlayerChanged(AssociatedPlayer? player) {
        this.nearestPlayer = player;

        if (player == null) {
            if (this.opponentLocked) this.opponentLocked = false;
            SetText("NEAREST PLAYER", "None found.");
            return;
        }

        string playerName = PlayerNameFilter.DoFilter(player.SlopPlayer.Name);
        SetText("NEAREST PLAYER", playerName);

        nameDisplaySequence!.Restart();
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

        ActiveEncounter = encounter;
        return true;
    }

    private void SendCancelEncounterRequest(EncounterType encounter) {
        // TODO
        /*Plugin.NetworkConnection.SendMessage(new ServerboundEncounterCancel {
            EncounterType = encounter
        });*/
    }

    public void HandleEncounterRequest(ClientboundEncounterRequest request) {
        if (!this.config.Phone.ReceiveNotifications.Value) return;
        if (this.encounterManager.CurrentEncounter?.IsBusy == true) return;

        if (this.playerManager.Players.TryGetValue(request.PlayerId, out var associatedPlayer)) {
            var me = WorldHandler.instance.GetCurrentPlayer();
            if (me == null) return;

            PreviousEncounterRequest = request;

            var phone = me.phone;
            var emailApp = phone.GetAppInstance<AppEmail>();
            var emailNotif = emailApp.GetComponent<Notification>();
            this.SetNotification(emailNotif);

            var name = PlayerNameFilter.DoFilter(associatedPlayer.SlopPlayer.Name);
            phone.PushNotification(this, name, null);
        }
    }

    private void SetNotification(Notification notif) {
        if (this.notificationInitialized) return;
        var newNotif = Instantiate(notif.gameObject, this.transform);
        this.m_Notification = newNotif.GetComponent<Notification>();
        this.m_Notification.InitNotification(this);
        this.notificationInitialized = true;
    }

    private void SetEncounterStatus(EncounterStatus status) {
        var button = scrollView!.GetButtonByRelativeIndex((int) ActiveEncounter) as EncounterButton;
        if (button == null) return;

        if (button.Status == status) {
            return;
        }

        button.SetStatus(status);
    }

    public void EndWaitingForEncounter() {
        isWaitingForEncounter = false;
        SetEncounterStatus(EncounterStatus.None);
    }

    private bool HasBannedMods() {
        var bannedMods = this.serverConfig.Hello?.BannedPlugins ?? new();
        return Chainloader.PluginInfos.Keys.Any(x => bannedMods.Contains(x));
    }
}
