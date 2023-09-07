using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BepInEx.Bootstrap;
using HarmonyLib;
using Reptile;
using Reptile.Phone;
using SlopCrew.Common;
using SlopCrew.Common.Network.Serverbound;
using TMPro;
using UnityEngine.UI;
using Vector3 = System.Numerics.Vector3;

namespace SlopCrew.Plugin.UI.Phone;

public class AppSlopCrew : App {
    public TMP_Text? Label;
    private AssociatedPlayer? nearestPlayer;
    private EncounterType encounterType = EncounterType.ScoreEncounter;

    private List<EncounterType> encounterTypes = new() {
        EncounterType.ScoreEncounter,
        EncounterType.ComboEncounter,
        EncounterType.GraffitiEncounter
    };

    private bool notifInitialized;
    private bool playerLocked;

    public override void Awake() {
        this.m_Unlockables = Array.Empty<AUnlockable>();
        base.Awake();
    }

    protected override void OnAppInit() {
        var homeScreen = this.MyPhone.GetAppInstance<AppHomeScreen>();
        var scrollView = Traverse.Create(homeScreen).Field<HomescreenScrollView>("m_ScrollView").Value;
        var traverse = Traverse.Create(scrollView);
        var upArrow = traverse.Field<Image>("m_ArrowUp").Value;
        var downArrow = traverse.Field<Image>("m_ArrowDown").Value;

        var ourUpArrow = Instantiate(upArrow.gameObject, this.transform);
        var ourDownArrow = Instantiate(downArrow.gameObject, this.transform);

        var half = (1775 / 2) - 100;
        ourUpArrow.transform.localPosition = new UnityEngine.Vector3(0, half, 0);
        ourDownArrow.transform.localPosition = new UnityEngine.Vector3(0, -half, 0);
    }

    public override void OnPressUp() {
        var nextIndex = this.encounterTypes.IndexOf(this.encounterType) - 1;
        if (nextIndex < 0) nextIndex = this.encounterTypes.Count - 1;
        this.encounterType = this.encounterTypes[nextIndex];
    }

    public override void OnPressDown() {
        var nextIndex = this.encounterTypes.IndexOf(this.encounterType) + 1;
        if (nextIndex >= this.encounterTypes.Count) nextIndex = 0;
        this.encounterType = this.encounterTypes[nextIndex];
    }

    public override void OnPressRight() {
        if (!this.SendEncounterRequest()) return;

        // People wanted an audible sound so you'll get one
        var audioManager = Core.Instance.AudioManager;
        var playSfx = AccessTools.Method("Reptile.AudioManager:PlaySfxGameplay",
                                         new[] {typeof(SfxCollectionID), typeof(AudioClipID), typeof(float)});
        playSfx.Invoke(audioManager, new object[] {SfxCollectionID.PhoneSfx, AudioClipID.FlipPhone_Confirm, 0f});
    }

    private bool SendEncounterRequest() {
        if (this.nearestPlayer == null) return false;
        if (Plugin.CurrentEncounter?.IsBusy() == true) return false;
        if (this.HasBannedMods()) return false;

        Plugin.Log.LogInfo("Sending encounter request " + this.encounterType);
        Plugin.NetworkConnection.SendMessage(new ServerboundEncounterRequest {
            PlayerID = this.nearestPlayer.SlopPlayer.ID,
            EncounterConfig = this.encounterType switch {
                EncounterType.ScoreEncounter => new ScoreEncounterConfig(),
                EncounterType.ComboEncounter => new ComboEncounterConfig(),
                EncounterType.GraffitiEncounter => new GraffitiEncounterConfig()
            }
        });
        return true;
    }

    private void ConfirmEncounterRequest(EncounterConfig receivedConfig) {
        Plugin.NetworkConnection.SendMessage(new ServerboundEncounterRequest {
            PlayerID = this.nearestPlayer.SlopPlayer.ID,
            EncounterConfig = receivedConfig
        });
    }

    public override void OnAppUpdate() {
        var me = WorldHandler.instance.GetCurrentPlayer();
        if (me is null || this.Label is null) return;

        if (this.HasBannedMods()) {
            this.Label.text = "Please disable\ntrick mods";
            return;
        }

        if (Plugin.CurrentEncounter?.IsBusy() == true) {
            this.Label.text = "glhf";
            return;
        }

        if (!this.playerLocked) {
            var position = me.transform.position.FromMentalDeficiency();
            this.nearestPlayer = Plugin.PlayerManager.AssociatedPlayers
                .Where(x => x.IsValid())
                .OrderBy(x =>
                             Vector3.Distance(
                                 x.ReptilePlayer.transform.position.FromMentalDeficiency(),
                                 position
                             ))
                .FirstOrDefault();
        }

        if (this.nearestPlayer == null) {
            if (this.playerLocked) this.playerLocked = false;
            this.Label.text = "No players nearby";
        } else {
            var modeName = this.encounterType switch {
                EncounterType.ScoreEncounter => "score",
                EncounterType.ComboEncounter => "combo",
                EncounterType.GraffitiEncounter => "graffiti"
            };

            var filteredName = PlayerNameFilter.DoFilter(this.nearestPlayer.SlopPlayer.Name);
            var text = $"Press right\nto {modeName} battle\n" + filteredName;

            if (this.playerLocked) {
                text = $"{filteredName}<color=white>\nwants to {modeName} battle!\n\nPress right\nto start";
            }

            this.Label.text = text;
        }
    }

    private bool HasBannedMods() {
        var bannedMods = new List<string> {
            "us.wallace.plugins.BRC.TiltTricking",
            "TrickGod",
            "BumperCars"
        };
        return Chainloader.PluginInfos.Keys.Any(x => bannedMods.Contains(x));
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
            this.encounterType = request.EncounterConfig.Type;

            if (Plugin.PlayerManager.Players.TryGetValue(request.PlayerID, out var player)) {
                this.nearestPlayer = player;
                if (Plugin.SlopConfig.StartEncountersOnRequest.Value) {
                    this.ConfirmEncounterRequest(request.EncounterConfig);
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
