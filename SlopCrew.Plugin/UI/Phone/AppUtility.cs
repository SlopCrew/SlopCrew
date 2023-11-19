using Reptile;
using Reptile.Phone;
using TMPro;
using UnityEngine;

namespace SlopCrew.Plugin.UI.Phone;

public static class AppUtility {
    // Width = Full width, there's nothing in the way of an app horizontally
    // Height = Phone screen height - the status at the top (160 in size)
    public static readonly Vector2 AppSize = new Vector2(1070, 1740);

    public static T CreateApp<T>(string name, Transform root) where T : App {
        var appObject = new GameObject(name);
        appObject.layer = Layers.Phone;

        var appRect = appObject.AddComponent<RectTransform>();
        appRect.SetParent(root, false);
        // We need to set the size manually as the app parent is of size 0
        appRect.sizeDelta = AppSize;
        // Align the app to the top
        // We don't have to compensate for the status bar here because the app parent already does this
        appRect.SetAnchorAndPivot(0.5f, 1.0f);

        var contentObject = new GameObject("Content");
        contentObject.layer = Layers.Phone;
        contentObject.transform.SetParent(appRect, false);

        var contentRect = contentObject.AddComponent<RectTransform>();
        contentRect.StretchToFillParent();

        var app = appObject.AddComponent<T>();

        return app;
    }

    /// <summary>
    /// Creates an app title bar with a name and icon, like the base apps in Bomb Rush Cyberfunk.
    /// </summary>
    public static RectTransform CreateAppOverlay(AppMusicPlayer sourceApp,
                                        bool useFooter,
                                        RectTransform root,
                                        string title,
                                        Sprite icon,
                                        out RectTransform header,
                                        out RectTransform footer,
                                        RectTransform? view = null) {
        // Overlay
        var overlay = sourceApp.transform.Find("Content/Overlay");
        var newOverlay = Object.Instantiate(overlay, root) as RectTransform;
        newOverlay!.StretchToFillParent();

        var titleLabel = newOverlay!.Find("Icons/HeaderLabel").GetComponent<TextMeshProUGUI>();
        Object.Destroy(titleLabel.GetComponent<TMProLocalizationAddOn>());
        titleLabel.SetText(title);

        header = (RectTransform) newOverlay.transform.Find("OverlayTop");
        header.SetAnchorAndPivot(0.5f, 1.0f);
        header.anchoredPosition = Vector2.zero;

        footer = (RectTransform) newOverlay.transform.Find("OverlayBottom");
        if (useFooter) {
            footer!.SetAnchorAndPivot(0.5f, 0.0f);
            footer!.anchoredPosition = Vector2.zero;
        } else {
            Object.Destroy(footer!.gameObject);
        }
        var iconImage = newOverlay.transform.Find("Icons/AppIcon").GetComponent<UnityEngine.UI.Image>();
        iconImage.sprite = icon;

        if (view != null) {
            if (useFooter) view.offsetMin = new Vector2(view.offsetMin.x, -footer.sizeDelta.y);
            view.offsetMax = new Vector2(view.offsetMax.x, -header.sizeDelta.y);
        }

        return newOverlay;
    }
}
