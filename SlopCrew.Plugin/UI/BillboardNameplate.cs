using System;
using HarmonyLib;
using Reptile;
using TMPro;
using UnityEngine;

namespace SlopCrew.Plugin.UI;

public class BillboardNameplate : TextMeshPro {
    public AssociatedPlayer AssociatedPlayer;

    protected override void Awake() {
        base.Awake();

        // Yoink the font from somewhere else because I guess asset loading is impossible
        var uiManager = Core.Instance.UIManager;
        var gameplay = Traverse.Create(uiManager).Field<GameplayUI>("gameplay").Value;
        this.font = gameplay.trickNameLabel.font;

        this.alignment = TextAlignmentOptions.Midline;
        this.fontSize = 2.5f;
    }

    // Billboard the nameplates
    private void Update() {
        if (!Plugin.ConfigBillboardNameplates.Value) return;
        var camera = WorldHandler.instance.CurrentCamera;
        if (camera is null) return;

        var rot = Quaternion.LookRotation(camera.transform.forward, Vector3.up);
        rot = Quaternion.Euler(0, rot.eulerAngles.y, 0);
        this.gameObject.transform.rotation = rot;
    }
}
