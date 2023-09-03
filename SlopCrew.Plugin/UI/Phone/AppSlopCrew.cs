using System;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using Reptile;
using Reptile.Phone;
using SlopCrew.Common;
using SlopCrew.Common.Network.Serverbound;
using TMPro;
using Vector3 = System.Numerics.Vector3;

namespace SlopCrew.Plugin.UI.Phone;

public class AppSlopCrew : App {
    public TMP_Text? Label;
    private AssociatedPlayer? nearestPlayer;
    private EncounterType encounterType = EncounterType.ScoreEncounter;
    private bool notifInitialized;
    private bool playerLocked;

    public override void Awake() {
        this.m_Unlockables = Array.Empty<AUnlockable>();
        base.Awake();
    }

    public override void OnPressUp() {
        this.encounterType = this.encounterType == EncounterType.ScoreEncounter
                                 ? EncounterType.ComboEncounter
                                 : EncounterType.ScoreEncounter;
    }

    public override void OnPressDown() {
        this.encounterType = this.encounterType == EncounterType.ScoreEncounter
                                 ? EncounterType.ComboEncounter
                                 : EncounterType.ScoreEncounter;
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
        Plugin.NetworkConnection.SendMessage(new ServerboundEncounterRequest {
            PlayerID = this.nearestPlayer.SlopPlayer.ID,
            EncounterType = this.encounterType
        });
        return true;
    }

    public override void OnAppUpdate() {
        var me = WorldHandler.instance.GetCurrentPlayer();
        if (me is null || this.Label is null) return;

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
                EncounterType.ComboEncounter => "combo"
            };

            var filteredName = PlayerNameFilter.DoFilter(this.nearestPlayer.SlopPlayer.Name);
            var text = $"Press right\nto {modeName} battle\n" + filteredName;

            if (this.playerLocked) {
                text = $"{filteredName}<color=white>\nwants to {modeName} battle!\n\nPress right\nto start";
            }

            this.Label.text = text;
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
            this.encounterType = request.EncounterType;

            if (Plugin.PlayerManager.Players.TryGetValue(request.PlayerID, out var player)) {
                this.nearestPlayer = player;
                if (Plugin.SlopConfig.StartEncountersOnRequest.Value) {
                    this.SendEncounterRequest();
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
