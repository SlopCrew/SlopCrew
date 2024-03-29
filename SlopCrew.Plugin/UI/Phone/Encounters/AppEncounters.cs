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
    public bool HasNearbyPlayer => this.closestPlayer != null;

    public static ClientboundEncounterRequest? PreviousEncounterRequest;

    private EncounterView? scrollView;

    private AssociatedPlayer? closestPlayer;
    private bool isWaitingForEncounter = false;

    private bool opponentLocked;
    private bool hasBannedMods;
    private bool notificationInitialized;

    private Image? bigTextBackground;
    private TextMeshProUGUI? bigTextTitleLabel;
    private TextMeshProUGUI? bigTextMessageLabel;
    private bool isShowingBigText;
    private Sequence? bigTextSequence;

    private TextMeshProUGUI? titleLabel;
    private TextMeshProUGUI? opponentNameLabel;
    private float initialOpponentNameX;
    private Sequence? nameDisplaySequence;

    private EncounterManager encounterManager = null!;
    private ConnectionManager connectionManager = null!;
    private PlayerManager playerManager = null!;
    private Config config = null!;
    private ServerConfig serverConfig = null!;

    private const string ClosestPlayerTitle = "CLOSEST PLAYER";

    public override void OnAppInit() {
        this.encounterManager = Plugin.Host.Services.GetRequiredService<EncounterManager>();
        this.connectionManager = Plugin.Host.Services.GetRequiredService<ConnectionManager>();
        this.playerManager = Plugin.Host.Services.GetRequiredService<PlayerManager>();
        this.config = Plugin.Host.Services.GetRequiredService<Config>();
        this.serverConfig = Plugin.Host.Services.GetRequiredService<ServerConfig>();

        this.scrollView = ExtendedPhoneScroll.Create<EncounterView>("EncounterScrollView", this, this.Content);
        this.AddOverlay();
        this.AddBigText();

        this.NearestPlayerChanged(null);
    }

    private void AddBigText() {
        var musicApp = this.MyPhone.GetAppInstance<AppMusicPlayer>();
        var musicButtonPrefab = musicApp.m_TrackList.m_AppButtonPrefab;

        var confirmArrow = musicButtonPrefab.transform.Find("PromptArrow");

        var textContainer = musicApp.transform.Find("Content/StatusPanel/RightSide");
        var label = textContainer.Find("CurrentTitleLabel").GetComponent<TextMeshProUGUI>();

        var bigTextBackgroundObject = new GameObject("Big Text");
        this.bigTextBackground = bigTextBackgroundObject.AddComponent<Image>();
        this.bigTextBackground.color = Color.clear;
        var bigTextBackgroundRect = this.bigTextBackground.rectTransform;
        bigTextBackgroundRect.SetParent(this.Content, false);
        bigTextBackgroundRect.SetSiblingIndex(this.scrollView!.transform.GetSiblingIndex() + 1);
        bigTextBackgroundRect.StretchToFillParent();
        // Offset for overlay
        var scrollViewRect = this.scrollView.RectTransform();
        bigTextBackgroundRect.offsetMin = scrollViewRect.offsetMin;
        bigTextBackgroundRect.offsetMax = scrollViewRect.offsetMax;

        float textSize = this.MyPhone.ScreenSize.x - 64.0f;
        float textPosition = 64.0f;

        this.bigTextTitleLabel = new GameObject("Title Label").AddComponent<TextMeshProUGUI>();
        this.bigTextTitleLabel.font = label.font;
        this.bigTextTitleLabel.fontSize = label.fontSize * 1.25f;
        this.bigTextTitleLabel.fontSharedMaterial = label.fontSharedMaterial;
        this.bigTextTitleLabel.alpha = 0.0f;
        var bigTextTitleRect = this.bigTextTitleLabel.rectTransform;
        bigTextTitleRect.SetParent(bigTextBackgroundRect, false);
        bigTextTitleRect.SetAnchorAndPivot(0.0f, 1.0f);
        bigTextTitleRect.sizeDelta = new Vector2(textSize + 256.0f, this.bigTextTitleLabel.fontSize);
        bigTextTitleRect.anchoredPosition = new Vector2(textPosition + 256.0f, -64.0f);

        this.bigTextMessageLabel = Instantiate(this.bigTextTitleLabel, bigTextBackgroundRect);
        this.bigTextMessageLabel.fontSize = label.fontSize;
        var bigTextMessageRect = this.bigTextMessageLabel.rectTransform;
        bigTextMessageRect.anchoredPosition =
            new Vector2(textPosition + 256.0f, bigTextTitleRect.anchoredPosition.y - 128.0f);

        // Arrow to indicate pressing right = confirm
        var arrow = (RectTransform) Instantiate(confirmArrow);
        arrow.SetParent(bigTextBackgroundRect, false);
        arrow.SetAnchorAndPivot(1.0f, 0.0f);
        arrow.anchoredPosition = new Vector2(-64, 64.0f);
        arrow.gameObject.SetActive(true);

        this.bigTextSequence = DOTween.Sequence();
        this.bigTextSequence.SetAutoKill(false);
        this.bigTextSequence.Append(this.bigTextBackground.DOFade(0.95f, 0.2f));
        this.bigTextSequence.Append(this.bigTextTitleLabel.DOFade(1.0f, 0.1f));
        this.bigTextSequence.Join(bigTextTitleRect.DOAnchorPosX(textPosition, 0.1f));
        this.bigTextSequence.Append(this.bigTextMessageLabel.DOFade(1.0f, 0.1f));
        this.bigTextSequence.Join(bigTextMessageRect.DOAnchorPosX(textPosition, 0.1f));

        this.bigTextBackground!.gameObject.SetActive(false);
    }

    private void AddOverlay() {
        var musicApp = this.MyPhone.GetAppInstance<AppMusicPlayer>();

        AppUtility.CreateAppOverlay(musicApp, true, this.Content, "Activities", AppSlopCrew.SpriteSheet.MainIcon,
                                    out _, out RectTransform footer, this.scrollView.RectTransform());

        // Status panel
        var statusPanel = musicApp.transform.Find("Content/StatusPanel").gameObject;

        var textContainer = statusPanel.transform.Find("RightSide");
        var label = textContainer.Find("CurrentTitleLabel").GetComponent<TextMeshProUGUI>();

        this.titleLabel = new GameObject("Title Label").AddComponent<TextMeshProUGUI>();
        this.titleLabel.font = label.font;
        this.titleLabel.fontSize = label.fontSize;
        this.titleLabel.fontSharedMaterial = label.fontSharedMaterial;
        var titleRect = this.titleLabel.rectTransform;
        titleRect.SetParent(footer, false);
        titleRect.SetAnchorAndPivot(0.0f, 1.0f);
        titleRect.sizeDelta = new Vector2(footer.sizeDelta.x - 32.0f, this.titleLabel.fontSize);
        titleRect.anchoredPosition = new Vector2(32.0f, -165.0f);

        this.opponentNameLabel = Instantiate(this.titleLabel, footer);
        this.opponentNameLabel.alpha = 0.0f;
        var opponentNameRect = this.opponentNameLabel.rectTransform;
        opponentNameRect.anchoredPosition =
            new Vector2(titleRect.anchoredPosition.x + 256.0f, titleRect.anchoredPosition.y - 70.0f);

        this.initialOpponentNameX = titleRect.anchoredPosition.x;

        this.nameDisplaySequence = DOTween.Sequence();
        this.nameDisplaySequence.SetAutoKill(false);
        this.nameDisplaySequence.Join(this.opponentNameLabel.rectTransform.DOAnchorPosX(this.initialOpponentNameX, 0.2f));
        this.nameDisplaySequence.Join(this.opponentNameLabel.DOFade(1.0f, 0.2f));
    }

    public override void OnAppTerminate() {
        if (this.nameDisplaySequence != null) {
            this.nameDisplaySequence.Kill();
        }
        if (this.bigTextSequence != null) {
            this.bigTextSequence.Kill();
        }
    }

    public override void OnAppEnable() {
        base.OnAppEnable();

        this.PlayEnableAnimation();

        this.hasBannedMods = this.HasBannedMods();
        if (this.hasBannedMods) {
            this.scrollView!.CanvasGroup!.alpha = 0.5f;
            this.SetText("Please disable advantageous mods.", string.Empty);
            foreach (var button in this.scrollView.GetButtons()) {
                button.IsSelected = false;
            }
            this.scrollView.enabled = false;

            return;
        }
    }

    private void PlayEnableAnimation() {
        this.nameDisplaySequence.Restart();
        Sequence enableAnimation = DOTween.Sequence();

        for (int i = 0; i < this.scrollView!.GetScrollRange(); i++) {
            var button = (EncounterButton) this.scrollView.GetButtonByRelativeIndex(i);
            RectTransform buttonRect = button.RectTransform();

            var targetPosition = this.scrollView.GetButtonPosition(i, buttonRect);
            if (i != this.scrollView.GetSelectorPos())
                targetPosition.x = 70.0f;
            buttonRect.anchoredPosition = new Vector2(this.MyPhone.ScreenSize.x, buttonRect.anchoredPosition.y);
            enableAnimation.Append(buttonRect.DOAnchorPos(targetPosition, 0.08f));
        }
    }

    public override void OnAppUpdate() {
        var player = WorldHandler.instance.GetCurrentPlayer();
        if (player is null)
            return;

        if (this.encounterManager.CurrentEncounter is RaceEncounter
            && this.isWaitingForEncounter
            && this.ActiveEncounter == EncounterType.Race) {
            this.EndWaitingForEncounter();
        }

        if (this.hasBannedMods)
            return;

        if (this.encounterManager.CurrentEncounter?.IsBusy == true) {
            if (this.encounterManager.CurrentEncounter is RaceEncounter race && race.IsWaitingForResults()) {
                this.SetEncounterStatus(EncounterStatus.WaitingResults);
            } else {
                this.SetEncounterStatus(EncounterStatus.InProgress);
            }
            return;
        }

        // This happens when literally no encounter is going on or one just ended
        if (!this.isWaitingForEncounter)
            this.SetEncounterStatus(EncounterStatus.None);

        if (!this.opponentLocked) {
            var position = player.transform.position;
            var newNearestPlayer = this.playerManager.AssociatedPlayers
                .Where(x => x.ReptilePlayer != null)
                .OrderBy(x => Vector3.Distance(x.ReptilePlayer.transform.position, position))
                .FirstOrDefault();

            if (newNearestPlayer != this.closestPlayer) {
                this.NearestPlayerChanged(newNearestPlayer);
            }
        }
    }

    public override void OpenContent(AUnlockable unlockable, bool appAlreadyOpen) {
        if (PreviousEncounterRequest is not null) {
            if (this.playerManager.Players.TryGetValue(PreviousEncounterRequest.PlayerId, out var player)) {
                this.closestPlayer = player;

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

        this.scrollView!.Move(PhoneScroll.ScrollDirection.Previous, this.m_AudioManager);
    }

    public override void OnPressDown() {
        if (this.hasBannedMods) {
            return;
        }

        this.scrollView!.Move(PhoneScroll.ScrollDirection.Next, this.m_AudioManager);
    }

    public override void OnPressRight() {
        if (this.isShowingBigText) {
            return;
        }

        if (this.hasBannedMods) {
            return;
        }

        var contentIndex = this.scrollView!.GetContentIndex();
        var currentSelectedMode = (EncounterType) contentIndex;

        var selectedButton = (EncounterButton) this.scrollView!.SelectedButtton;
        if (selectedButton!.Unavailable) {
            return;
        }

        if (this.isWaitingForEncounter && currentSelectedMode != this.ActiveEncounter) {
            return;
        }

        this.scrollView!.HoldAnimationSelectedButton();
    }

    public override void OnReleaseRight() {
        if (this.isShowingBigText) {
            this.DismissBigText();
            return;
        }

        if (this.hasBannedMods) {
            return;
        }

        this.scrollView!.ActivateAnimationSelectedButton();

        var contentIndex = this.scrollView!.GetContentIndex();
        var currentSelectedMode = (EncounterType) contentIndex;

        var selectedButton = (EncounterButton) this.scrollView!.SelectedButtton;
        if (selectedButton.Unavailable) {
            return;
        }

        if (this.isWaitingForEncounter && currentSelectedMode == this.ActiveEncounter) {
            this.SendCancelEncounterRequest(this.ActiveEncounter);
            return;
        }

        if (!this.SendEncounterRequest(currentSelectedMode))
            return;

        this.m_AudioManager.PlaySfxUI(SfxCollectionID.PhoneSfx, AudioClipID.FlipPhone_Confirm);

        if (currentSelectedMode == EncounterType.Race && !this.isWaitingForEncounter) {
            this.ActiveEncounter = EncounterType.Race;
            this.isWaitingForEncounter = true;
            this.SetEncounterStatus(EncounterStatus.WaitingStart);
        }
    }

    private void SetText(string title, string messsage) {
        if (this.titleLabel!.text != title) {
            this.titleLabel.SetText(title);
        }
        if (this.opponentNameLabel!.text != messsage) {
            this.opponentNameLabel.SetText(messsage);
        }
    }

    public void SetBigText(string title, string message) {
        this.isShowingBigText = true;

        this.bigTextBackground!.gameObject.SetActive(true);

        this.bigTextTitleLabel!.SetText(title);
        this.bigTextMessageLabel!.SetText(message);

        this.bigTextSequence.Restart();
    }

    private void DismissBigText() {
        this.isShowingBigText = false;

        this.bigTextBackground!.gameObject.SetActive(false);

        this.m_AudioManager.PlaySfxUI(SfxCollectionID.PhoneSfx, AudioClipID.FlipPhone_Confirm);
    }

    private void NearestPlayerChanged(AssociatedPlayer? player) {
        this.closestPlayer = player;

        if (player == null) {
            if (this.opponentLocked)
                this.opponentLocked = false;
            this.SetText(ClosestPlayerTitle, "None found.");
            return;
        }


        var playerName = PlayerNameFilter.DoFilter(player.SlopPlayer.Name);
        playerName = PlayerNameFilter.CloseAll(playerName);
        this.SetText(ClosestPlayerTitle, playerName);

        this.nameDisplaySequence!.Restart();
    }

    private bool SendEncounterRequest(EncounterType encounter) {
        // TODO: races
        if (encounter is not EncounterType.Race && this.closestPlayer == null)
            return false;
        if (this.encounterManager.CurrentEncounter?.IsBusy == true)
            return false;
        if (this.hasBannedMods)
            return false;

        var id = this.closestPlayer?.SlopPlayer.Id;

        var encounterRequest = new ServerboundEncounterRequest {
            Type = encounter
        };
        if (id is not null)
            encounterRequest.PlayerId = id.Value;

        this.connectionManager.SendMessage(new ServerboundMessage {
            EncounterRequest = encounterRequest
        });

        this.ActiveEncounter = encounter;
        return true;
    }

    private void SendCancelEncounterRequest(EncounterType encounter) {
        // TODO
        /*Plugin.NetworkConnection.SendMessage(new ServerboundEncounterCancel {
            EncounterType = encounter
        });*/
    }

    public void HandleEncounterRequest(ClientboundEncounterRequest request) {
        if (!this.config.Phone.ReceiveNotifications.Value)
            return;
        if (this.encounterManager.CurrentEncounter?.IsBusy == true)
            return;

        if (this.playerManager.Players.TryGetValue(request.PlayerId, out var associatedPlayer)) {
            var me = WorldHandler.instance.GetCurrentPlayer();
            if (me == null)
                return;

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
        if (this.notificationInitialized)
            return;
        var newNotif = Instantiate(notif.gameObject, this.transform);
        this.m_Notification = newNotif.GetComponent<Notification>();
        this.m_Notification.InitNotification(this);
        this.notificationInitialized = true;
    }

    private void SetEncounterStatus(EncounterStatus status) {
        var button = this.scrollView!.GetButtonByRelativeIndex((int) this.ActiveEncounter) as EncounterButton;
        if (button == null)
            return;

        if (button.Status == status) {
            return;
        }

        button.SetStatus(status);
    }

    public void EndWaitingForEncounter() {
        this.isWaitingForEncounter = false;
        this.SetEncounterStatus(EncounterStatus.None);
    }

    private bool HasBannedMods() {
        var bannedMods = this.serverConfig.Hello?.BannedPlugins ?? new();
        return Chainloader.PluginInfos.Keys.Any(x => bannedMods.Contains(x));
    }
}
