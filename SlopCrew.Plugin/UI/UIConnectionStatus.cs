using System;
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
        tmp = obj.AddComponent<TextMeshProUGUI>();

        // We love stealing shit
        var uiManager = Core.Instance.UIManager;
        var gameplay = Traverse.Create(uiManager).Field<GameplayUI>("gameplay").Value;
        var rep = gameplay.repLabel;

        this.tmp.font = rep.font;
        this.tmp.fontSize = 45;
        this.tmp.fontMaterial = rep.fontMaterial;

        // epic positioning
        this.tmp.alignment = TextAlignmentOptions.TopLeft;
        var rect = this.tmp.rectTransform;

        // this goes top left, because x0 y0 is bottom right in Unity :^) - jay
        rect.anchorMin = new Vector2(0, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(0.2f, 0.9f);

        this.tmp.rectTransform.SetParent(gameplay.gameplayScreen.GetComponent<RectTransform>(), false);
    }

    private void Update() {
        var player = WorldHandler.instance.GetCurrentPlayer();
        if (player is null || !player.isActiveAndEnabled) {
            this.tmp.enabled = false;
            return;
        }

        var phone = Traverse.Create(player).Field<Phone>("phone").Value;
        this.tmp.enabled = phone.transform.Find("OpenCanvas").gameObject.activeSelf;
    }

    private void FixedUpdate() {
        var connStatus = Plugin.API.Connected ? "<color=green>True" : "<color=red>False";
        this.tmp.text = $"Connected: {connStatus}<color=white>\nPlayers: {Plugin.API.PlayerCount}";
    }
}
