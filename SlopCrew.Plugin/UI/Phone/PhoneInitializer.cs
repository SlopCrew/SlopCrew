using Reptile;
using UnityEngine;

namespace SlopCrew.Plugin.UI.Phone; 

public class PhoneInitializer {
    // We need to shove our own GameObject with a AppSlopCrew component into the prefab
    public void InitPhone(Player instance) {
        var prefab = instance.phonePrefab;
        var apps = prefab.transform.Find("OpenCanvas/PhoneContainerOpen/MainScreen/Apps");

        var slopApp = new GameObject("AppSlopCrew");
        slopApp.AddComponent<AppSlopCrew>();
        slopApp.transform.SetParent(apps);
    }
}
