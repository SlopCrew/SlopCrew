using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using HarmonyLib;
using Reptile;
using SlopCrew.Plugin.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Transform = SlopCrew.Common.Transform;

namespace SlopCrew.Plugin;

public class AssociatedPlayer {
    public Common.Player SlopPlayer;
    public Reptile.Player ReptilePlayer;
    public MapPin? MapPin;

    public Queue<Transform> TransformUpdates = new Queue<Transform>();
    public Transform TargetTransform = new Transform();
    public Transform PrevTarget = new Transform();
    public Vector3 FromPosition;
    public Quaternion FromRotation;
    public float TimeElapsed;
    public float TimeToTarget;
    public float LerpAmount;

    public AssociatedPlayer(Common.Player slopPlayer) {
        this.SlopPlayer = slopPlayer;
        this.ReptilePlayer = PlayerManager.SpawnReptilePlayer(slopPlayer);

        // NEED TO REDO THIS
        //this.StartPos = slopPlayer.Position.ToMentalDeficiency();
        //this.TargetPos = slopPlayer.Position.ToMentalDeficiency();
        this.FromPosition = slopPlayer.Position.ToMentalDeficiency();

        if (Plugin.ConfigShowPlayerNameplates.Value) {
            this.SpawnNameplate();
        }

        if (Plugin.ConfigShowPlayerPins.Value) {
            this.SpawnMapPin();
        }
    }

    private void SpawnNameplate() {
        var container = new GameObject("SlopCrew_NameplateContainer");

        // Setup the nameplate itself
        var nameplate = new GameObject("SlopCrew_Nameplate");
        var tmp = nameplate.AddComponent<TextMeshPro>();
        tmp.text = this.SanitizeNameplate(this.SlopPlayer.Name);

        // Yoink the font from somewhere else because I guess asset loading is impossible
        var uiManager = Core.Instance.UIManager;
        var gameplay = Traverse.Create(uiManager).Field<GameplayUI>("gameplay").Value;
        tmp.font = gameplay.trickNameLabel.font;

        tmp.alignment = TextAlignmentOptions.Midline;
        tmp.fontSize = 2.5f;

        nameplate.transform.parent = container.transform;

        if (this.SlopPlayer.IsDeveloper) {
            var heat = gameplay.wanted1;
            var icon = heat.GetComponent<Image>();

            var devIcon = new GameObject("SlopCrew_DevIcon"); 
            devIcon.name = "SlopCrew_DevIcon";
            devIcon.SetActive(true);
            
            var spriteRenderer = devIcon.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = icon.sprite;

            // center it
            var localPosition = devIcon.transform.localPosition;
            localPosition -= new Vector3(0, localPosition.y / 2, 0);
            devIcon.transform.localPosition = localPosition;
            
            devIcon.transform.parent = container.transform;
            devIcon.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            devIcon.transform.position += new Vector3(0, 0.25f, 0);
            devIcon.AddComponent<UISpinny>();
        }

        // Configure the container's position
        var bounds = this.ReptilePlayer.interactionCollider.bounds;
        container.transform.position = new Vector3(
            bounds.center.x,
            bounds.max.y + 0.125f,
            bounds.center.z
        );

        // Rotate it to match the player's head
        container.transform.rotation = this.ReptilePlayer.tf.rotation;
        // and flip it around
        container.transform.Rotate(0, 180, 0);

        container.transform.parent = this.ReptilePlayer.interactionCollider.transform;
        container.AddComponent<UINameplate>();
    }

    private string SanitizeNameplate(string original) {
        var regex = new Regex("<size.*?>");
        return regex.Replace(original, "");
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

    public void SetPos(SlopCrew.Common.Transform tf, uint currentTick) {
        if (this.ReptilePlayer is not null) {
            tf.Tick = currentTick;
            this.TransformUpdates.Enqueue(tf);
        }
    }
    
    public void InterpolatePosition() {
        var target = this.TargetTransform.Position.ToMentalDeficiency();
        var newPos = Vector3.zero;

        if (this.TargetTransform.Stopped) {
            // If player is stopped just lerp to the target position
            newPos = Vector3.Lerp(this.FromPosition, target, this.LerpAmount);
        } else if ((target - this.FromPosition).magnitude > 10f) {
            // Teleport them to the target position if they happen to get too far away
            this.ReptilePlayer.motor.RigidbodyMove(target);
        } else {
            // Interpolate to the target position
            newPos = Vector3.LerpUnclamped(this.FromPosition, target, this.LerpAmount);
        }

        //Plugin.Log.LogInfo("MOVING: FROM: " + this.FromPosition + " TO: " + target + " BY: " + this.LerpAmount);
        this.ReptilePlayer.motor.RigidbodyMove(newPos);
    }

    public void InterpolateRotation() {
        var target = this.TargetTransform.Rotation.ToMentalDeficiency();
        Quaternion newRot;
        
        if (this.TargetTransform.Stopped) {
            newRot = Quaternion.Lerp(this.FromRotation, target, this.LerpAmount);
        } else {
            newRot = Quaternion.SlerpUnclamped(this.FromRotation, target, this.LerpAmount);
        }
        
        this.ReptilePlayer.motor.RigidbodyMoveRotation(newRot.normalized);
    }
}
