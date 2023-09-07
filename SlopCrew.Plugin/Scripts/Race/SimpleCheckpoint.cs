using Reptile;
using UnityEngine;

namespace SlopCrew.Plugin.Scripts {
    internal class SimpleCheckpoint : MonoBehaviour {
        private BoxCollider? collider;
        private bool? isTriggered;
        private MapPin? mapPin;

        private void Start() {
            isTriggered = false;

            collider = gameObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(15.5f, 100.5f, 15.5f); //TODO: fix Y size
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
