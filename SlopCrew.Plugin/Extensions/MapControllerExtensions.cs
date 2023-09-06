using HarmonyLib;
using Reptile;
using SlopCrew.Plugin.Scripts;
using SlopCrew.Plugin.Scripts.Race;
using System.Collections.Generic;
using UnityEngine;
using static Reptile.Player;

namespace SlopCrew.Plugin.Extensions {
    public static class MapControllerExtensions {
        public static CheckpointPin InstantiateRaceCheckPoint(this Reptile.Mapcontroller mapController, System.Numerics.Vector3 position) {
            //Map pin
            var pin = Traverse.Create(mapController)
                                  .Method("CreatePin", MapPin.PinType.StoryObjectivePin)
                                  .GetValue<MapPin>();

            var cp = new GameObject("SimpleCP");
            cp.transform.position = position.ToMentalDeficiency();
            var compo = UnityEngine.Object.Instantiate<GameObject>(cp).AddComponent<SimpleCheckpoint>();

            pin.AssignGameplayEvent(cp);
            pin.InitMapPin(Reptile.MapPin.PinType.StoryObjectivePin);
            pin.OnPinEnable();

            compo.SetMapPin(pin);
            compo.SetPosition(position.ToMentalDeficiency());

            //Ui indicator
            UIIndicatorData uiIndicatorData = new Reptile.Player.UIIndicatorData();
            uiIndicatorData.trans = cp.transform;
            uiIndicatorData.isActive = false;

            var currentPlayer = WorldHandler.instance.GetCurrentPlayer();

            var phone = Traverse.Create(currentPlayer).Field("phone").GetValue<Reptile.Phone.Phone>();
            var storySpotUI = Traverse.Create(phone).Field("storySpotUI").GetValue<GameObject>();
            var obj = UnityEngine.Object.Instantiate(storySpotUI);
            obj.transform.SetParent(phone.dynamicGameplayScreen, worldPositionStays: false);
            obj.transform.localScale = Vector2.one;
            obj.SetActive(false);
            uiIndicatorData.uiObject = obj;
            uiIndicatorData.SetComponents();
            uiIndicatorData.timeTillShow = 0f;

            return new CheckpointPin {
                Pin = pin,
                UIIndicator = uiIndicatorData
            };
        }

        public static void UpdateOriginalPins(this Mapcontroller mapcontroller, bool shouldEnable) {
            foreach (var pin in Traverse.Create(mapcontroller).Field("m_MapPins").GetValue<List<MapPin>>()) {
                Traverse.Create(pin).Field("isActiveAndEnabled").SetValue(shouldEnable);
            }
            foreach (var pin in Traverse.Create(mapcontroller).Field("storyObjectivePins").GetValue<MapPin[]>()) {
                Traverse.Create(pin).Field("isActiveAndEnabled").SetValue(shouldEnable);
            }
        }
    }
}
