using Reptile;
using Reptile.Phone;
using UnityEngine;

namespace SlopCrew.Plugin.UI.Phone;

public abstract class ExtendedPhoneScroll : PhoneScroll {
    public static T Create<T>(string name, App associatedApp, RectTransform root) where T : ExtendedPhoneScroll {
        var viewObject = new GameObject(name);
        viewObject.layer = Layers.Phone;

        Vector2 viewSize = new(1070, 1775);
        var rect = viewObject.AddComponent<RectTransform>();
        rect.SetParent(root, false);
        rect.SetAnchorAndPivot(1.0f, 0.5f);
        rect.sizeDelta = viewSize;

        var view = viewObject.AddComponent<T>();
        view.Initialize(associatedApp, root);

        return view;
    }

    public abstract void Initialize(App associatedApp, RectTransform root);

    public virtual void Show() {
        this.gameObject.SetActive(true);
    }
    public virtual void Hide(System.Action callback) {
        this.gameObject.SetActive(false);
        callback?.Invoke();
    }
}
