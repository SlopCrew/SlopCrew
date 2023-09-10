using System;
using HarmonyLib;
using Reptile;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SlopCrew.Plugin.Encounters.Race;

public class CheckpointPin : IDisposable {
    public GameObject GameObject;
    public MapPin Pin;
    public Player.UIIndicatorData UIIndicator { get; set; } = new();

    private DateTime disableOccludedAt = DateTime.MinValue;

    private const float MaxDistance = 5000f;

    public CheckpointPin() {
        this.GameObject = new GameObject(RaceCheckpoint.Tag);
        this.Pin = this.GameObject.AddComponent<MapPin>();
    }

    public void Dispose() {
        Object.Destroy(this.GameObject);
    }

    public void UpdateUIIndicator() {
        if (!UIIndicator.isActive) return;

        var currentPlayer = WorldHandler.instance.GetCurrentPlayer();

        var uIIndicatorPos = UIIndicator.trans.position;
        UIIndicator.inView = Traverse.Create(currentPlayer)
            .Method("InView", UIIndicator, uIIndicatorPos)
            .GetValue<bool>();

        if (UIIndicator.inView) {
            var cam = Traverse.Create(currentPlayer).Field("cam").GetValue<GameplayCamera>();
            var realTf = Traverse.Create(cam).Field("realTf").GetValue<Transform>();

            var camPosition = realTf.position;
            var direction = uIIndicatorPos - camPosition;

            if (
                Physics.Raycast(camPosition, direction, out var hitInfo,
                                MaxDistance, currentPlayer.uiIndicatorOcclusionLayerMask,
                                QueryTriggerInteraction.Collide)
            ) {
                if (hitInfo.distance > 5f && DateTime.UtcNow >= disableOccludedAt) {
                    UIIndicator.isOccluded = false;
                    disableOccludedAt = DateTime.UtcNow.AddSeconds(3);
                } else {
                    UIIndicator.isOccluded = hitInfo.collider.transform != UIIndicator.trans;
                }
            }

            Traverse.Create(currentPlayer)
                .Method("UpdateUIIndicatorAnimation", UIIndicator.inView, UIIndicator, Vector3.zero)
                .GetValue();
        }
    }

    public void Activate() {
        Pin.gameObject.SetActive(true);
        UIIndicator.isActive = true;
        disableOccludedAt = DateTime.UtcNow;
    }

    public void Deactivate() {
        Pin.gameObject.SetActive(false);
        UIIndicator.isActive = false;
        UIIndicator.uiObject.SetActive(false);
    }
}
