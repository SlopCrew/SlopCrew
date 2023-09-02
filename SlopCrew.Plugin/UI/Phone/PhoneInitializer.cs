using HarmonyLib;
using Reptile;
using UnityEngine;

namespace SlopCrew.Plugin.UI.Phone;

public class PhoneInitializer {
    // We need to shove our own GameObject with a AppSlopCrew component into the prefab
    public void InitPhone(Player instance) {
        var prefab = instance.phonePrefab;
        var apps = prefab.transform.Find("OpenCanvas/PhoneContainerOpen/MainScreen/Apps");

        var slopAppObj = new GameObject("AppSlopCrew");
        slopAppObj.layer = Layers.Phone;

        var contentObj = new GameObject("Content");
        contentObj.layer = Layers.Phone;

        var app = slopAppObj.AddComponent<AppSlopCrew>();
        var content = contentObj.AddComponent<RectTransform>();
        var tmp = contentObj.AddComponent<TMPro.TextMeshProUGUI>();

        // seems to be a hardcoded size
        content.sizeDelta = new(1070, 1775);
        tmp.rectTransform.sizeDelta = content.sizeDelta;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;

        // Same shit we do for the nameplate
        var uiManager = Core.Instance.UIManager;
        var gameplay = Traverse.Create(uiManager).Field<GameplayUI>("gameplay").Value;
        tmp.font = gameplay.trickNameLabel.font;
        tmp.fontSize = 100f;

        app.Label = tmp;

        slopAppObj.transform.SetParent(apps);
        contentObj.transform.SetParent(slopAppObj.transform);

        // why are these zero? idk!
        slopAppObj.transform.localScale = new(1, 1, 1);
        contentObj.transform.localScale = new(1, 1, 1);
    }
}
