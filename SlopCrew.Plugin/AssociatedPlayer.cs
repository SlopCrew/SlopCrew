using System.Collections.Generic;
using HarmonyLib;
using Reptile;
using SlopCrew.Common.Network.Clientbound;
using SlopCrew.Plugin.UI;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using Object = UnityEngine.Object;

namespace SlopCrew.Plugin;

public class AssociatedPlayer {
    public Common.Player SlopPlayer;
    public Reptile.Player ReptilePlayer;
    public MapPin? MapPin;

    public Vector3 StartPos;
    public Vector3 TargetPos;
    public Quaternion StartRot;
    public Quaternion TargetRot;
    public float TimeElapsed;

    public AssociatedPlayer(Common.Player slopPlayer) {
        this.SlopPlayer = slopPlayer;
        this.ReptilePlayer = PlayerManager.SpawnReptilePlayer(slopPlayer);

        this.StartPos = slopPlayer.Position.ToMentalDeficiency();
        this.TargetPos = slopPlayer.Position.ToMentalDeficiency();

        if (Plugin.ConfigShowPlayerNameplates.Value) {
            this.SpawnNameplate();
        }

        if (Plugin.ConfigShowPlayerPins.Value) {
            this.SpawnMapPin();
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

    private void SpawnMapPin() {
        var mapController = Mapcontroller.Instance;
        this.MapPin = Traverse.Create(mapController)
                              .Method("CreatePin", MapPin.PinType.StoryObjectivePin)
                              .GetValue<MapPin>();

        this.MapPin.AssignGameplayEvent(this.ReptilePlayer.gameObject);
        this.MapPin.InitMapPin(MapPin.PinType.StoryObjectivePin);
        this.MapPin.OnPinEnable();

        var pinInObj = this.MapPin.transform.Find("InViewVisualization").gameObject;

        // Particles. Get rid of them.
        var pinInPartObj = pinInObj.transform.Find("Particle System").gameObject;
        Object.Destroy(pinInPartObj);

        var pinOutObj = this.MapPin.transform.Find("OutOfViewVisualization").gameObject;
        var pinOutPartS = pinOutObj.GetComponent<ParticleSystem>();
        var pinOutPartR = pinOutObj.GetComponent<ParticleSystemRenderer>();
        Object.Destroy(pinOutPartS);
        Object.Destroy(pinOutPartR);

        // Color
        var pinInMeshR = pinInObj.GetComponent<MeshRenderer>();
        var pinInMat = pinInMeshR.material;
        pinInMat.color = new Color(1f, 1f, 0.85f);
    }

    public void FuckingObliterate() {
        WorldHandler.instance.SceneObjectsRegister.players.Remove(this.ReptilePlayer);

        if (this.ReptilePlayer.gameObject is not null)
            Object.Destroy(this.ReptilePlayer.gameObject);

        if (this.ReptilePlayer is not null)
            Object.Destroy(this.ReptilePlayer);

        if (this.MapPin?.gameObject is not null)
            Object.Destroy(this.MapPin.gameObject);

        if (this.MapPin is not null)
            Object.Destroy(this.MapPin);
    }

    public void ResetPlayer(Common.Player slopPlayer) {
        this.SlopPlayer = slopPlayer;
        //this.FuckingObliterate();
        //this.ReptilePlayer = PlayerManager.SpawnReptilePlayer(slopPlayer);
    }

    public void SetPos(ClientboundPlayerPositionUpdate posUpdate) {
        if (this.ReptilePlayer is not null) {
            this.StartPos = this.ReptilePlayer.motor.BodyPosition();
            this.TargetPos = posUpdate.Position.ToMentalDeficiency();
            this.StartRot = this.ReptilePlayer.motor.rotation;
            this.TargetRot = posUpdate.Rotation.ToMentalDeficiency();
            this.TimeElapsed = 0f;
        }
    }
}
