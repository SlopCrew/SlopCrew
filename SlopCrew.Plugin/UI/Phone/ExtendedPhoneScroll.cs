using Reptile;
using Reptile.Phone;
using UnityEngine;

namespace SlopCrew.Plugin.UI.Phone;

public abstract class ExtendedPhoneScroll : PhoneScroll {
    public static T Create<T>(string name, App associatedApp, RectTransform root) where T : ExtendedPhoneScroll {
        var viewObject = new GameObject(name);
        viewObject.layer = Layers.Phone;

        var rect = viewObject.AddComponent<RectTransform>();
        rect.SetParent(root, false);
        rect.StretchToFillParent();

        var view = viewObject.AddComponent<T>();
        view.Initialize(associatedApp, root);

        return view;
    }

    public abstract void Initialize(App associatedApp, RectTransform root);
}
