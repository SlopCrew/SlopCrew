using Reptile;
using UnityEngine;

namespace SlopCrew.Plugin.Encounters.Race;

public class RaceCheckpoint : MonoBehaviour {
    public const string Tag = "SlopCrew_RaceCheckpoint";

    private BoxCollider collider = null!;
    private bool isTriggered;
    private MapPin mapPin = null!;

    private void Start() {
        collider = gameObject.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = new Vector3(5.5f, 10.5f, 5.5f);
    }

    private void OnTriggerEnter(Collider other) {
        if (isTriggered) return;
        if (Plugin.CurrentEncounter is not SlopRaceEncounter raceEncounter) return;

        // Don't trigger if race hasn't started
        if (raceEncounter.IsStarting()) {
            return;
        }

        if (other != WorldHandler.instance.GetCurrentPlayer().interactionCollider) {
            return;
        }

        var res = raceEncounter.OnCheckpointReached(mapPin);
        if (res) isTriggered = true;
    }

    public void SetPosition(Vector3 pos) {
        transform.position = pos;
    }

    public void SetMapPin(MapPin newPin) {
        this.mapPin = newPin;
    }
}
