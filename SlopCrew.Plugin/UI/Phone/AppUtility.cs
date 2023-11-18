using Reptile;
using Reptile.Phone;
using UnityEngine;

namespace SlopCrew.Plugin.UI.Phone;

public static class AppUtility {
    public static T Create<T>(string name, Transform root) where T : App {
        var appObject = new GameObject(name);
        appObject.layer = Layers.Phone;

        var appRect = appObject.AddComponent<RectTransform>();
        appRect.SetParent(root, false);
        appRect.sizeDelta = Vector2.zero;
        appRect.anchorMin = Vector2.zero;
        appRect.anchorMax = Vector2.one;

        var contentObject = new GameObject("Content");
        contentObject.layer = Layers.Phone;
        contentObject.transform.SetParent(appRect, false);
        contentObject.transform.localScale = Vector3.one;

        var contentRect = contentObject.AddComponent<RectTransform>();
        contentRect.sizeDelta = Vector2.zero;
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;

        var app = appObject.AddComponent<T>();

        return app;
    }
}
