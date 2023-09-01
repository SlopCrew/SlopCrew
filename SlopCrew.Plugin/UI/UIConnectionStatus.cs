using HarmonyLib;
using Reptile;
using Reptile.Phone;
using TMPro;
using UnityEngine;

namespace SlopCrew.Plugin.UI;

public class UIConnectionStatus : MonoBehaviour {
    private TextMeshProUGUI tmp = null!;

    private void Awake() {
        var obj = new GameObject("SlopCrew_UIConnectionStatus");
        this.tmp = obj.AddComponent<TextMeshProUGUI>();

        var player = WorldHandler.instance.GetCurrentPlayer();
        var phone = Traverse.Create(player).Field<Reptile.Phone.Phone>("phone").Value;
        var clockLabel = Traverse.Create(phone.Statusbar).Field<TextMeshProUGUI>("m_ClockLabel").Value;

        this.tmp.font = clockLabel.font;
        this.tmp.fontSize = clockLabel.fontSize * 0.65f;
        this.tmp.fontMaterial = clockLabel.fontMaterial;
        this.tmp.alignment = TextAlignmentOptions.BottomRight;
        
        var rect = this.tmp.rectTransform;
        rect.anchorMin = new Vector2(0.85f, 0.335f);
        rect.anchorMax = new Vector2(0.85f, 0.335f);

        var appHomeScreen = phone.AppInstances["AppHomeScreen"];
        var topView = Traverse.Create(appHomeScreen).Field<RectTransform>("m_TopView").Value;
        this.tmp.rectTransform.SetParent(topView, false);
    }

    private void FixedUpdate() {
        var color = Plugin.API.Connected ? "green" : "red";
        var emoji = Plugin.API.Connected ? 14 : 15;
        this.tmp.text = $"<color={color}>-<color=white>" +          // dash - connection indicator
                        $"<sprite=\"EmojiOne\" index={emoji}>\n" +  // emoji :^)
                        $"{Plugin.API.PlayerCount}";
    }
}
