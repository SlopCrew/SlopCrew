using HarmonyLib;
using Reptile;
using SlopCrew.Plugin.Scripts;
using UnityEngine;

namespace SlopCrew.Plugin.Extensions {
    public static class MapControllerExtensions {
        public static MapPin InstantiateRacePin(this Reptile.Mapcontroller mapController, System.Numerics.Vector3 position) {
            var pin = Traverse.Create(mapController)
                                  .Method("CreatePin", MapPin.PinType.StoryObjectivePin)
                                  .GetValue<MapPin>();

            var cp = new GameObject("SimpleCP");
            cp.transform.position = position.ToMentalDeficiency();
            var compo = UnityEngine.Object.Instantiate<GameObject>(cp).AddComponent<SimpleCheckpoint>();

            pin.SetMapController(mapController);
            pin.AssignGameplayEvent(cp);
            pin.InitMapPin(Reptile.MapPin.PinType.StoryObjectivePin);
            pin.OnPinEnable();

            compo.SetMapPin(pin);
            compo.SetPosition(position.ToMentalDeficiency());

            return pin;
        }
    }
}
