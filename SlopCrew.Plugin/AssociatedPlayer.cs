using System;
using Reptile;
using DG.Tweening;
using HarmonyLib;
using SlopCrew.Common.Network.Clientbound;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace SlopCrew.Plugin;

public class AssociatedPlayer {
    public Common.Player SlopPlayer;
    public Reptile.Player ReptilePlayer;

    public Vector3 startPos;
    public Vector3 targetPos;
    public Quaternion startRot;
    public Quaternion targetRot;
    public float timeElapsed;

    public AssociatedPlayer(Common.Player slopPlayer) {
        this.SlopPlayer = slopPlayer;
        this.ReptilePlayer = PlayerManager.SpawnReptilePlayer(slopPlayer);
        this.SpawnNameplate();
    }

    private void SpawnNameplate() {
        var obj = new GameObject("SlopCrew_Nameplate");
        var tmp = obj.AddComponent<TextMeshPro>();

        // Yoink the font from somewhere else because I guess asset loading is impossible
        var uiManager = Core.Instance.UIManager;
        var gameplay = Traverse.Create(uiManager).Field<GameplayUI>("gameplay").Value;
        var font = gameplay.trickNameLabel.font;

        tmp.text = this.SlopPlayer.Name;
        tmp.alignment = TextAlignmentOptions.Midline;
        tmp.font = font;
        tmp.fontSize = 2.5f;

        var bounds = this.ReptilePlayer.interactionCollider.bounds;
        obj.transform.position = new Vector3(
            bounds.center.x,
            bounds.max.y + 0.125f,
            bounds.center.z
        );

        // Rotate it to match the player's head
        obj.transform.rotation = this.ReptilePlayer.tf.rotation;
        // and flip it around
        obj.transform.Rotate(0, 180, 0);

        obj.transform.parent = this.ReptilePlayer.interactionCollider.transform;
    }

    public void FuckingObliterate() {
        WorldHandler.instance.SceneObjectsRegister.players.Remove(this.ReptilePlayer);

        if (this.ReptilePlayer.gameObject is not null)
            Object.Destroy(this.ReptilePlayer.gameObject);

        if (this.ReptilePlayer is not null)
            Object.Destroy(this.ReptilePlayer);
    }

    public void ResetPlayer(Common.Player slopPlayer) {
        this.SlopPlayer = slopPlayer;
        //this.FuckingObliterate();
        //this.ReptilePlayer = PlayerManager.SpawnReptilePlayer(slopPlayer);
    }

    public void SetPos(ClientboundPlayerPositionUpdate posUpdate) {
        if (this.ReptilePlayer is not null) {
            this.startPos = this.ReptilePlayer.motor.BodyPosition();
            this.targetPos = posUpdate.Position.ToMentalDeficiency();
            this.startRot = this.ReptilePlayer.motor.rotation;
            this.targetRot = posUpdate.Rotation.ToMentalDeficiency();
            this.timeElapsed = 0f;
        }
    }
}
