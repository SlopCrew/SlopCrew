using Reptile;
using UnityEngine;

namespace SlopCrew.Plugin.Scripts {
    internal class SimpleCheckpoint : MonoBehaviour {
        private BoxCollider? collider;
        private bool? isTriggered;
        private MapPin? mapPin;
        public static string TAG = "Checkpoint";

        private void Start() {
            isTriggered = false;

            collider = gameObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(5.5f, 10.5f, 5.5f);
        }

        private void OnTriggerEnter(Collider other) {

            //Dont trigger if race hasn't started
            if (!Plugin.RaceManager!.HasStarted()) {
                return;
            }

            //Dont trigger if not the current player
            if (other.name != WorldHandler.instance.GetCurrentPlayer().name) {
                return;
            }

            //Dont trigger if already triggered
            if (isTriggered.HasValue && isTriggered.Value) {
                return;
            }

            var res = Plugin.RaceManager.OnCheckpointReached(mapPin);
            if (res) {
                isTriggered = true;
            }
        }

        public void SetPosition(Vector3 pos) {
            transform.position = pos;
        }

        public void SetMapPin(MapPin mapPin) {
            this.mapPin = mapPin;
        }
    }
}
