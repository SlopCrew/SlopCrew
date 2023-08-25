using Reptile;
using SlopCrew.Common.Network.Clientbound;
using SlopCrew.Plugin.UI;
using UnityEngine;
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
        if (Plugin.ConfigShowPlayerNameplates.Value) {
            this.SpawnNameplate();
        }
    }

    private void SpawnNameplate() {
        var obj = new GameObject("SlopCrew_Nameplate");
        var tmp = obj.AddComponent<BillboardNameplate>();
        tmp.text = this.SlopPlayer.Name;
        tmp.AssociatedPlayer = this;

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
