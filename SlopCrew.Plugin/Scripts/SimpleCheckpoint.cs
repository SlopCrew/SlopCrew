using Reptile;
using UnityEngine;

namespace SlopCrew.Plugin.Scripts {
    internal class SimpleCheckpoint : MonoBehaviour {
        private BoxCollider _collider;
        private bool _isTriggered;
        private MapPin _mapPin;

        private void Start() {
            //transform.position = Pos;
            _isTriggered = false;

            _collider = gameObject.AddComponent<BoxCollider>();
            _collider.isTrigger = true;
            _collider.size = new Vector3(3.5f, 100.5f, 3.5f);

            Plugin.Log.LogInfo("New CP");
        }

        private void OnTriggerEnter(Collider other) {

            //Dont trigger if race hasn't started
            if (!RaceManager.Instance.HasStarted()) {
                return;
            }

            //Dont trigger if not the current player
            if (other.name != WorldHandler.instance.GetCurrentPlayer().name) {
                return;
            }

            //Dont trigger if already triggered
            if (_isTriggered) {
                return;
            }

            Plugin.Log.LogInfo($"Checkpoint reached {other.name}");

            var res = RaceManager.Instance.OnCheckpointReached(_mapPin);
            if (res) {
                _isTriggered = true;
            }
        }

        public void SetPosition(Vector3 pos) {
            transform.position = pos;
        }

        public void SetMapPin(MapPin mapPin) {
            _mapPin = mapPin;
        }
    }
}
