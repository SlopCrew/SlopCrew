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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlopCrew.Plugin.UI.Phone;

public class AppSlopCrew : App {
    [NonSerialized] public string? RaceRankings;

    private SlopCrewScrollView? scrollView;

    private AssociatedPlayer? nearestPlayer;
    private EncounterType waitingEncounter;
    private bool isWaitingForEncounter = false;

    private EncounterType[] encounterTypes = (EncounterType[]) Enum.GetValues(typeof(EncounterType));

    private bool notifInitialized;
    private bool playerLocked;
    private bool hasBannedMods;

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

        AddScrollView(content);
        AddOverlay(content);
    }

    private void AddOverlay(RectTransform content) {
        AppGraffiti graffitiApp = this.MyPhone.GetAppInstance<AppGraffiti>();

        GameObject overlay = graffitiApp.transform.GetChild(1).gameObject;
        GameObject slopOverlay = Instantiate(overlay, content);

        var title = slopOverlay.GetComponentInChildren<TextMeshProUGUI>();
        Destroy(title.GetComponent<TMProLocalizationAddOn>());
        Destroy(title.GetComponent<TMProFontLocalizer>());
        title.SetText("Slop Crew");

        var overlayHeaderImage = slopOverlay.transform.GetChild(0);
        overlayHeaderImage.localPosition = Vector2.up * 870.0f;

        var iconImage = slopOverlay.transform.GetChild(1).GetChild(1).GetComponent<Image>();
        iconImage.sprite = TextureLoader.LoadResourceAsSprite("SlopCrew.Plugin.res.phone_icon.png", 128, 128);
    }

    private void AddScrollView(RectTransform content) {
        AppMusicPlayer musicApp = this.MyPhone.GetAppInstance<AppMusicPlayer>();

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
        hasBannedMods = HasBannedMods();
        if (hasBannedMods) {
            scrollView.CanvasGroup.alpha = 0.5f;
        }
    }

    public override void OnPressUp() {
        if (this.RaceRankings is not null) {
            this.RaceRankings = null;
            return;
        }

        scrollView.Move(PhoneScroll.ScrollDirection.Previous, m_AudioManager);
    }

    public override void OnPressDown() {
        if (this.RaceRankings is not null) {
            this.RaceRankings = null;
            return;
        }

        scrollView.Move(PhoneScroll.ScrollDirection.Next, m_AudioManager);
    }

    public override void OnPressRight() {
        if (hasBannedMods) {
            return;
        }

        if (nearestPlayer == null) {
            return;
        }

        EncounterType currentSelectedMode = (EncounterType) scrollView.GetContentIndex();

        if (isWaitingForEncounter && currentSelectedMode != waitingEncounter) {
            return;
        }

        scrollView.HoldAnimationSelectedButton();
    }

    public override void OnReleaseRight() {
        scrollView.ActivateAnimationSelectedButton();

        EncounterType currentSelectedMode = (EncounterType) scrollView.GetContentIndex();

        if (isWaitingForEncounter && currentSelectedMode == waitingEncounter) {
            SendCancelEncounterRequest(waitingEncounter);
            return;
        }

        if (this.RaceRankings is not null) {
            this.RaceRankings = null;
            return;
        }

        if (!SendEncounterRequest(currentSelectedMode)) return;

        m_AudioManager.PlaySfx(SfxCollectionID.PhoneSfx, AudioClipID.FlipPhone_Confirm);

        if (currentSelectedMode == EncounterType.RaceEncounter && !isWaitingForEncounter) {
            waitingEncounter = EncounterType.RaceEncounter;
            isWaitingForEncounter = true;
            SetWaiting(true);
        }
    }

    private bool SendEncounterRequest(EncounterType encounter) {
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
        Plugin.NetworkConnection.SendMessage(new ServerboundEncounterCancel {
            EncounterType = encounter
        });
    }

    public override void OnAppUpdate() {
        var player = WorldHandler.instance.GetCurrentPlayer();
        if (player is null) return;

        if (Plugin.CurrentEncounter is SlopRaceEncounter && isWaitingForEncounter && waitingEncounter == EncounterType.RaceEncounter) {
            EndWaitingForEncounter();
        }

        if (isWaitingForEncounter && waitingEncounter == EncounterType.RaceEncounter) {
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
                //this.Label.text = "Waiting for results...";
            } else {
                //this.Label.text = "glhf";
            }
            return;
        }

        if (this.RaceRankings is not null) {
            //this.Label.text = this.RaceRankings;
            return;
        }

        if (!this.playerLocked) {
            var position = player.transform.position;
            this.nearestPlayer = Plugin.PlayerManager.AssociatedPlayers
                .Where(x => x.IsValid())
                .OrderBy(x => Vector3.Distance(x.ReptilePlayer.transform.position, position))
                .FirstOrDefault();
        }

        //if (this.encounter.IsStateful()) {
        //    this.Label.text = $"Press right\nto wait for a\n{modeName} battle";
        //    return;
        //}

        if (this.nearestPlayer == null) {
            if (this.playerLocked) this.playerLocked = false;
            //this.Label.text = $"No players nearby\nfor {modeName} battle";
        } else {
            var filteredName = PlayerNameFilter.DoFilter(this.nearestPlayer.SlopPlayer.Name);
            //var text = $"Press right\nto {modeName} battle\n" + filteredName;

            if (this.playerLocked) {
                //text = $"{filteredName}<color=white>\nwants to {modeName} battle!\n\nPress right\nto start";
            }

            //this.Label.text = text;
        }
    }

    private bool HasBannedMods() {
        var bannedMods = Plugin.NetworkConnection.ServerConfig?.BannedMods ?? new();
        return Chainloader.PluginInfos.Keys.Any(x => bannedMods.Contains(x));
    }

    public void EndWaitingForEncounter() {
        isWaitingForEncounter = false;

        SetWaiting(false);
    }

    private void SetWaiting(bool waiting) {
        var button = scrollView.GetButtons()[(int) waitingEncounter] as SlopCrewButton;
        if (button == null) return;

        button.SetWaiting(waiting);
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
