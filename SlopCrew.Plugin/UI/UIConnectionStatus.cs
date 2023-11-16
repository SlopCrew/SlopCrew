using HarmonyLib;
using Reptile;
using SlopCrew.API;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlopCrew.Plugin.UI;

public class UIConnectionStatus : MonoBehaviour {
    private Image icon = null!;
    private readonly Vector2 iconPos = new(0.875f, 0.335f);
    
    private TextMeshProUGUI tmp = null!;
    private RectTransform tmpRectTransform = null!;
    private readonly Vector2 counterPosMin = new(0.75f, 0.425f);
    private readonly Vector2 counterPosMax = new(1f, 0.425f);
    private readonly Vector2 warnPosMin = new(0f, 0.335f);
    private readonly Vector2 warnPosMax = new(1f, 0.335f);
    
    private bool showInfo = true;

    private void Awake() {
        // [Info   : Unity Log] Can't add 'TextMeshProUGUI' to SlopCrew_UIConnectionStatus because a 'Image' is already added to the game object!
        // A GameObject can only contain one 'Graphic' component.
        //
        // thank you unity, very cool! - jay
        
        // Icon
        var sprite = TextureLoader.LoadResourceAsSprite("SlopCrew.Plugin.res.player_icon.png", 128, 128);
        var iconObj = new GameObject("SlopCrew_UIConnectionStatus_Icon");
        this.icon = iconObj.AddComponent<Image>();
        this.icon.sprite = sprite;
        
        // Text
        var tmpObj = new GameObject("SlopCrew_UIConnectionStatus_Counter");
        this.tmp = tmpObj.AddComponent<TextMeshProUGUI>();

        var uiManager = Core.Instance.UIManager;
        var gameplay = uiManager.gameplay;
        var repLabel = gameplay.repLabel;

        this.tmp.color = Color.white;
        this.tmp.font = repLabel.font;
        this.tmp.fontSize = repLabel.fontSize * 0.7f;
        this.tmp.fontMaterial = repLabel.fontMaterial;
        this.tmp.alignment = TextAlignmentOptions.Bottom;
        
        // Placement
        var iconRectTransform = icon.RectTransform();
        iconRectTransform.anchorMin = this.iconPos;
        iconRectTransform.anchorMax = this.iconPos;
        
        this.tmpRectTransform = this.tmp.rectTransform;
        this.tmpRectTransform.anchorMin = this.counterPosMin;
        this.tmpRectTransform.anchorMax = this.counterPosMax;

        var player = WorldHandler.instance.GetCurrentPlayer();
        var phone = Traverse.Create(player).Field<Reptile.Phone.Phone>("phone").Value;
        var appHomeScreen = phone.AppInstances["AppHomeScreen"];
        var topView = Traverse.Create(appHomeScreen).Field<RectTransform>("m_TopView").Value;
        
        this.tmp.rectTransform.SetParent(topView, false);
        this.icon.rectTransform.SetParent(topView, false);
    }

    private void Update() {
        var api = APIManager.API!;
        
        if (!api.Connected && this.showInfo) {
            this.showInfo = false;
            this.icon.enabled = false;
            
            this.tmpRectTransform.anchorMin = this.warnPosMin;
            this.tmpRectTransform.anchorMax = this.warnPosMax;
            this.tmp.color = Color.red;
            this.tmp.text = "Connection lost...";
            
            return;
        }
        
        if (api.Connected && !this.showInfo) {
            this.showInfo = true;
            this.icon.enabled = true;
            
            this.tmpRectTransform.anchorMin = this.counterPosMin;
            this.tmpRectTransform.anchorMax = this.counterPosMax;
            this.tmp.color = Color.white;
        }
        
        if (this.showInfo) this.tmp.text = $"{api.PlayerCount}";
    }
}
