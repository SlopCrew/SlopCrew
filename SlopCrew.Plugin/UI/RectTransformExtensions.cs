using System.Drawing;
using UnityEngine;
using UnityEngine.UI;

namespace SlopCrew.Plugin.UI;

public static class RectTransformExtensions {
    public static void SetBounds(this RectTransform rect, float left, float top, float right, float bottom) {
        rect.offsetMin = new Vector2(-left, -top);
        rect.offsetMax = new Vector2(right, bottom);
    }

    public static void SetAnchor(this RectTransform rect, float x, float y) {
        Vector2 anchor = new Vector2(x, y);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
    }

    public static void SetPivot(this RectTransform rect, float x, float y) {
        rect.pivot = new Vector2(x, y);
    }

    public static void SetAnchorAndPivot(this RectTransform rect, float x, float y) {
        Vector2 point = new Vector2(x, y);
        rect.anchorMin = point;
        rect.anchorMax = point;
        rect.pivot = point;
    }
}
