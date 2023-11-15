using System;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using Reptile;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SlopCrew.Plugin.Encounters.Race;

public class RaceCheckpoint : MonoBehaviour {
    public const string Tag = "Checkpoint";

    private bool activated;

    public BoxCollider Collider = null!;
    public MapPin Pin = null!;
    public Player.UIIndicatorData UIIndicator = null!;

    private void Awake() {
        this.Collider = this.gameObject.AddComponent<BoxCollider>();
        this.Collider.isTrigger = true;
        this.Collider.size = new Vector3(5.5f, 10.5f, 5.5f);

        this.CreateUIIndicator();
        this.CreateMapPin();
    }

    private void OnDestroy() {
        if (this.Collider != null) Object.Destroy(this.Collider);
        if (this.Pin != null) Object.Destroy(this.Pin.gameObject);
        if (this.UIIndicator.uiObject != null) Object.Destroy(this.UIIndicator.uiObject);
    }

    private void CreateUIIndicator() {
        this.UIIndicator = new Player.UIIndicatorData();
        this.UIIndicator.trans = this.transform;
        this.UIIndicator.isActive = true;

        var player = WorldHandler.instance.GetCurrentPlayer();
        var obj = Instantiate(player.phone.storySpotUI, player.phone.dynamicGameplayScreen, false);
        obj.transform.localScale = Vector2.one;
        obj.SetActive(false);

        this.UIIndicator.uiObject = obj;
        this.UIIndicator.SetComponents();
    }

    private void CreateMapPin() {
        var mapController = Mapcontroller.Instance;
        this.Pin = mapController.CreatePin(MapPin.PinType.StoryObjectivePin);
        this.Pin.AssignGameplayEvent(this.gameObject);
        this.Pin.InitMapPin(MapPin.PinType.StoryObjectivePin);
        this.Pin.OnPinEnable();
    }

    private void Update() {
        this.UIIndicator.isActive = this.activated;
        this.Pin.gameObject.SetActive(this.activated);
    }

    public void UpdateUIIndicator() {
        if (!this.activated) this.Activate();

        var currentPlayer = WorldHandler.instance.GetCurrentPlayer();
        var uIIndicatorPos = this.UIIndicator.trans.position;

        this.UIIndicator.inView = currentPlayer.InView(this.UIIndicator, uIIndicatorPos);
        this.UIIndicator.isOccluded = true; // looks terrible otherwise

        currentPlayer.UpdateUIIndicatorAnimation(
            this.UIIndicator.inView,
            this.UIIndicator,
            Vector3.zero
        );
    }

    public void Activate() {
        this.Pin.gameObject.SetActive(true);
        this.UIIndicator.isActive = true;
        this.UIIndicator.uiObject.SetActive(true);
        this.Collider.enabled = true;

        this.activated = true;
    }

    public void Deactivate() {
        this.Pin.gameObject.SetActive(false);
        this.UIIndicator.isActive = false;
        this.UIIndicator.uiObject.SetActive(false);
        this.Collider.enabled = false;

        this.activated = false;
    }

    private void OnTriggerEnter(Collider other) {
        if (!this.activated) return;

        var encounterManager = Plugin.Host.Services.GetRequiredService<EncounterManager>();
        if (encounterManager.CurrentEncounter is not RaceEncounter raceEncounter) return;

        // Don't trigger if race hasn't started
        if (raceEncounter.IsStarting()) {
            return;
        }

        if (other.gameObject != WorldHandler.instance.GetCurrentPlayer().gameObject) return;
        
        var res = raceEncounter.OnCheckpointReached(this);
        if (res) this.Deactivate();
    }

    public void SetPosition(Vector3 pos) {
        this.transform.position = pos;
    }
}
