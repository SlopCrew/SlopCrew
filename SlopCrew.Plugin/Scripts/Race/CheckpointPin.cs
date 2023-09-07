using HarmonyLib;
using Reptile;
using UnityEngine;
using static Reptile.Player;

namespace SlopCrew.Plugin.Scripts.Race {
    public class CheckpointPin {
        public MapPin Pin { get; set; } = new MapPin();
        public UIIndicatorData UIIndicator { get; set; } = new UIIndicatorData();

        private readonly float MAX_DISTANCE = 5000f;

        public void UpdateUIIndicator() {
            if (UIIndicator is null || !UIIndicator.isActive) {
                return;
            }

            var currentPlayer = WorldHandler.instance.GetCurrentPlayer();

            var uIIndicatorPos = UIIndicator.trans.position;
            UIIndicator.inView = Traverse.Create(currentPlayer).Method("InView", UIIndicator, uIIndicatorPos).GetValue<bool>();
            if (UIIndicator.inView) {
                var cam = Traverse.Create(currentPlayer).Field("cam").GetValue<GameplayCamera>();
                var realTf = Traverse.Create(cam).Field("realTf").GetValue<UnityEngine.Transform>();

                var camPosition = realTf.position;
                var direction = uIIndicatorPos - camPosition;
                if (Physics.Raycast(camPosition, direction, out var hitInfo, MAX_DISTANCE, currentPlayer.uiIndicatorOcclusionLayerMask, QueryTriggerInteraction.Collide)) {
                    UIIndicator.isOccluded = hitInfo.collider.transform != UIIndicator.trans;
                }

                Traverse.Create(currentPlayer).Method("UpdateUIIndicatorAnimation", UIIndicator.inView, UIIndicator, Vector3.zero).GetValue();
            }
        }

        public void Activate() {
            Pin.gameObject.SetActive(true);
            UIIndicator.isActive = true;
        }

        public void Deactivate() {
            Pin.gameObject.SetActive(false);
            UIIndicator.isActive = false;
            UIIndicator.uiObject.SetActive(false);
        }
    }
}
