using System;
using HarmonyLib;
using Reptile;
using Reptile.Phone;
using UnityEngine;

namespace SlopCrew.Plugin.Encounters.Race;

public class RaceCheckpoint : MonoBehaviour {
    public const string Tag = "Checkpoint";

    private bool activated;

    public BoxCollider Collider = null!;
    public GameObject CheckpointObject;
    public MapPin Pin;
    public Player.UIIndicatorData UIIndicator;

    private void Awake() {
        this.Collider = this.gameObject.AddComponent<BoxCollider>();
        this.Collider.isTrigger = true;
        this.Collider.size = new Vector3(5.5f, 10.5f, 5.5f);

        this.CheckpointObject = new GameObject("RaceCheckpoint");
        this.CheckpointObject.tag = Tag;
        this.CheckpointObject.transform.position = this.gameObject.transform.position;

        this.CreateUIIndicator();
        this.CreateMapPin();
    }

    private void CreateUIIndicator() {
        this.UIIndicator = new Player.UIIndicatorData();
        this.UIIndicator.trans = this.transform;
        this.UIIndicator.isActive = true;

        var player = WorldHandler.instance.GetCurrentPlayer();
        var phone = Traverse.Create(player).Field("phone").GetValue<Phone>();
        var storySpotUI = Traverse.Create(phone).Field("storySpotUI").GetValue<GameObject>();
        var obj = Instantiate(storySpotUI, phone.dynamicGameplayScreen, false);
        obj.transform.localScale = Vector2.one;
        obj.SetActive(false);

        this.UIIndicator.uiObject = obj;
        this.UIIndicator.SetComponents();
    }

    private void CreateMapPin() {
        var mapController = Mapcontroller.Instance;
        this.Pin = Traverse.Create(mapController)
            .Method("CreatePin", MapPin.PinType.StoryObjectivePin)
            .GetValue<MapPin>();

        this.Pin.AssignGameplayEvent(this.CheckpointObject);
        this.Pin.InitMapPin(MapPin.PinType.StoryObjectivePin);
        this.Pin.OnPinEnable();
    }

    public void OnDestroy() {
        Destroy(this.Pin.gameObject);
        Destroy(this.UIIndicator.uiObject);
        Destroy(this.CheckpointObject);
    }

    private void Update() {
        this.UIIndicator.isActive = this.activated;
        this.Pin.gameObject.SetActive(this.activated);
    }

    public void UpdateUIIndicator() {
        if (!this.activated) this.Activate();

        var currentPlayer = WorldHandler.instance.GetCurrentPlayer();
        var uIIndicatorPos = this.UIIndicator.trans.position;

        this.UIIndicator.inView = Traverse.Create(currentPlayer)
            .Method("InView", this.UIIndicator, uIIndicatorPos)
            .GetValue<bool>();
        this.UIIndicator.isOccluded = true; // looks terrible otherwise

        Traverse.Create(currentPlayer)
            .Method("UpdateUIIndicatorAnimation", this.UIIndicator.inView, this.UIIndicator, Vector3.zero)
            .GetValue();
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

        if (Plugin.CurrentEncounter is not SlopRaceEncounter raceEncounter) return;

        // Don't trigger if race hasn't started
        if (raceEncounter.IsStarting()) {
            return;
        }

        if (other.gameObject != WorldHandler.instance.GetCurrentPlayer().gameObject) {
            Plugin.Log.LogInfo(other.name);
            return;
        }

        var res = raceEncounter.OnCheckpointReached(this);
        if (res) this.Deactivate();
    }

    public void SetPosition(Vector3 pos) {
        this.transform.position = pos;
    }
}
