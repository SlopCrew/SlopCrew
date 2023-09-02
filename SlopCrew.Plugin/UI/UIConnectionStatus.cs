using System;
using System.Reflection;
using HarmonyLib;
using Reptile;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlopCrew.Plugin.UI;

public class UIConnectionStatus : MonoBehaviour {
    // before someone strangles me for these vectors:
    // they are set in FixedUpdate().
    // doing new Vector2() 60 times a second is a horrible idea.
    
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
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SlopCrew.Plugin.res.player_icon.png");
        if (stream is null) throw new Exception("Could not load player icon");

        var bytes = new byte[stream.Length];
        var read = 0;
        while (read < bytes.Length) {
            read += stream.Read(bytes, read, bytes.Length - read);
        }

        var iconTex = new Texture2D(128, 128);
        iconTex.LoadImage(bytes);
        iconTex.Apply();
        var iconRect = new Rect(0, 0, iconTex.width, iconTex.height);
        var iconSpr = Sprite.Create(iconTex, iconRect, new Vector2(0.5f, 0.5f), 100);

        var iconObj = new GameObject("SlopCrew_UIConnectionStatus_Icon");
        this.icon = iconObj.AddComponent<Image>();
        this.icon.sprite = iconSpr;
        
        // Text
        var tmpObj = new GameObject("SlopCrew_UIConnectionStatus_Counter");
        this.tmp = tmpObj.AddComponent<TextMeshProUGUI>();

        // steal font info
        var uiManager = Core.Instance.UIManager;
        var gameplay = Traverse.Create(uiManager).Field<GameplayUI>("gameplay").Value;
        var steal = gameplay.repLabel;

        this.tmp.color = Color.white;
        this.tmp.font = steal.font;
        this.tmp.fontSize = steal.fontSize * 0.7f;
        this.tmp.fontMaterial = steal.fontMaterial;
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

    private void FixedUpdate() {
        // toggle visibility
        if (!Plugin.API.Connected && showInfo) {
            showInfo = false;
            this.icon.enabled = false;
            
            this.tmpRectTransform.anchorMin = this.warnPosMin;
            this.tmpRectTransform.anchorMax = this.warnPosMax;
            this.tmp.color = Color.red;
            this.tmp.text = "Connection lost...";
            
            return;
        }
        if (Plugin.API.Connected && !showInfo) {
            showInfo = true;
            this.icon.enabled = true;
            
            this.tmpRectTransform.anchorMin = this.counterPosMin;
            this.tmpRectTransform.anchorMax = this.counterPosMax;
            this.tmp.color = Color.white;
        }
        
        if (showInfo) {
            this.tmp.text = $"{Plugin.API.PlayerCount}";
        }
    }
}
