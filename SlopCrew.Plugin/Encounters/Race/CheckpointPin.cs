using System;
using HarmonyLib;
using Reptile;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SlopCrew.Plugin.Encounters.Race;

public class CheckpointPin : IDisposable {
    public MapPin Pin;
    public Player.UIIndicatorData UIIndicator;

    private DateTime disableOccludedAt = DateTime.MinValue;
    private const float MaxDistance = 5000f;

    public CheckpointPin(Vector3 pos) {
        var mapController = Mapcontroller.Instance;

        var pin = Traverse.Create(mapController)
            .Method("CreatePin", MapPin.PinType.StoryObjectivePin)
            .GetValue<MapPin>();

        var checkpoint = new GameObject("RaceCheckpoint");
        checkpoint.tag = RaceCheckpoint.Tag;
        checkpoint.transform.position = pos;
        var compo = Object.Instantiate(checkpoint).AddComponent<RaceCheckpoint>();

        pin.AssignGameplayEvent(checkpoint);
        pin.InitMapPin(MapPin.PinType.StoryObjectivePin);
        pin.OnPinEnable();

        compo.SetMapPin(pin);
        compo.SetPosition(pos);

        var indicatorData = new Player.UIIndicatorData();
        indicatorData.trans = checkpoint.transform;
        indicatorData.isActive = false;

        var currentPlayer = WorldHandler.instance.GetCurrentPlayer();

        var phone = Traverse.Create(currentPlayer).Field("phone").GetValue<Reptile.Phone.Phone>();
        var storySpotUI = Traverse.Create(phone).Field("storySpotUI").GetValue<GameObject>();

        var obj = Object.Instantiate(storySpotUI, phone.dynamicGameplayScreen, false);
        obj.transform.localScale = Vector2.one;
        obj.SetActive(false);

        indicatorData.uiObject = obj;
        indicatorData.SetComponents();
        indicatorData.timeTillShow = 0f;

        this.Pin = pin;
        this.UIIndicator = indicatorData;
    }

    public void Dispose() {
        Object.Destroy(this.Pin.gameObject);
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
