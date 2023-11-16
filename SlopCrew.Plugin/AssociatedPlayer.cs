using System;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using Reptile;
using SlopCrew.Common.Proto;
using SlopCrew.Plugin.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Vector3 = System.Numerics.Vector3;

namespace SlopCrew.Plugin;

public class AssociatedPlayer : IDisposable {
    public Common.Proto.Player SlopPlayer;
    public Reptile.Player ReptilePlayer;
    public MapPin? MapPin;

    private PlayerManager playerManager;
    private ConnectionManager connectionManager;
    private Config config;

    private UnityEngine.Vector3 velocity = new();
    private PositionUpdate? targetUpdate = null;
    private float? targetPosSpeed = null;
    private float? targetRotSpeed = null;

    public bool PhoneOut = false;

    private static readonly Color NamePlateOutlineColor = new Color(0.1f, 0.1f, 0.1f, 1.0f);
    private static Material? NameplateFontMaterial;
    private const float NameplateHeightFactor = 1.33f;

    // This is bad. I don't know what's better.
    public AssociatedPlayer(
        PlayerManager playerManager,
        ConnectionManager connectionManager,
        Config config,
        Common.Proto.Player slopPlayer
    ) {
        this.playerManager = playerManager;
        this.connectionManager = connectionManager;
        this.config = config;
        this.SlopPlayer = slopPlayer;

        var emptyGameObject = new GameObject("SlopCrew_EmptyGameObject");
        var emptyTransform = emptyGameObject.transform;
        emptyTransform.position = ((System.Numerics.Vector3) slopPlayer.Transform.Position).ToMentalDeficiency();
        emptyTransform.rotation = ((System.Numerics.Quaternion) slopPlayer.Transform.Rotation).ToMentalDeficiency();

        var character = (Characters) slopPlayer.CharacterInfo.Character;
        var outfit = slopPlayer.CharacterInfo.Outfit;
        var moveStyle = (MoveStyle) slopPlayer.CharacterInfo.MoveStyle;

        this.ReptilePlayer = WorldHandler.instance.SetupAIPlayerAt(
            emptyTransform,
            character,
            PlayerType.NONE,
            outfit,
            moveStyle
        );

        this.ReptilePlayer.motor.gravity = 0;
        this.ReptilePlayer.motor.SetKinematic(true);
        this.ReptilePlayer.motor.enabled = false;

        if (this.config.General.ShowPlayerNameplates.Value) {
            this.SpawnNameplate();
        }

        if (this.config.General.ShowPlayerMapPins.Value) {
            this.SpawnMapPin();
        }

        Object.Destroy(emptyGameObject);
    }

    // FIXME: nameplates sink into player in millenium square???
    private void SpawnNameplate() {
        var container = new GameObject("SlopCrew_NameplateContainer");

        // Setup the nameplate itself
        var nameplate = new GameObject("SlopCrew_Nameplate");
        var tmp = nameplate.AddComponent<TextMeshPro>();
        tmp.text = this.SlopPlayer.Name;
        nameplate.AddComponent<TextMeshProFilter>();

        // Yoink the font from somewhere else because I guess asset loading is impossible
        var gameplay = Core.Instance.UIManager.gameplay;
        tmp.font = gameplay.trickNameLabel.font;

        tmp.alignment = TextAlignmentOptions.Midline;
        tmp.fontSize = 2.5f;

        if (this.config.General.OutlineNameplates.Value) {
            // Lazy load the material so there's not a million material instances floating in memory
            if (NameplateFontMaterial == null) {
                NameplateFontMaterial = tmp.fontMaterial;
                NameplateFontMaterial.SetColor(ShaderUtilities.ID_OutlineColor, NamePlateOutlineColor);
                NameplateFontMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, NamePlateOutlineColor);
                NameplateFontMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.1f);
                NameplateFontMaterial.EnableKeyword(ShaderUtilities.Keyword_Underlay);
                NameplateFontMaterial.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.1f);
                NameplateFontMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.2f);
                NameplateFontMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.2f);
                NameplateFontMaterial.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.0f);
            }

            tmp.fontMaterial = NameplateFontMaterial;
        }

        nameplate.transform.parent = container.transform;

        if (this.SlopPlayer.IsCommunityContributor) {
            var heat = gameplay.wanted1;
            var icon = heat.GetComponent<Image>();

            var devIcon = new GameObject("SlopCrew_DevIcon");
            devIcon.name = "SlopCrew_DevIcon";
            devIcon.SetActive(true);

            var spriteRenderer = devIcon.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = icon.sprite;

            var localPosition = devIcon.transform.localPosition;
            localPosition -= new UnityEngine.Vector3(0, localPosition.y / 2, 0);
            devIcon.transform.localPosition = localPosition;

            devIcon.transform.parent = container.transform;
            devIcon.transform.localScale = new UnityEngine.Vector3(0.1f, 0.1f, 0.1f);
            devIcon.transform.position += new UnityEngine.Vector3(0, 0.25f, 0);
            devIcon.AddComponent<UISpinny>();
        }

        container.transform.SetParent(this.ReptilePlayer.interactionCollider.transform, false);
        nameplate.transform.SetParent(container.transform, false);

        var capsule = this.ReptilePlayer.interactionCollider as CapsuleCollider;
        // float * float before float * vector is faster because reasons
        container.transform.localPosition = UnityEngine.Vector3.up * (capsule!.height * NameplateHeightFactor);
        container.transform.Rotate(0, 180, 0);

        var uiNameplate = container.AddComponent<UINameplate>();
        uiNameplate.Billboard = this.config.General.BillboardNameplates.Value;
    }

    private void SpawnMapPin() {
        var mapController = Mapcontroller.Instance;
        this.MapPin = mapController.CreatePin(MapPin.PinType.StoryObjectivePin);

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

    public void Dispose() {
        if (this.ReptilePlayer != null) {
            var worldHandler = WorldHandler.instance;
            if (worldHandler != null) worldHandler.SceneObjectsRegister.players.Remove(this.ReptilePlayer);
            Object.Destroy(this.ReptilePlayer.gameObject);
        }

        if (this.MapPin != null) {
            Object.Destroy(this.MapPin.gameObject);
        }
    }

    public void HandleVisualUpdate(VisualUpdate update) {
        if (this.ReptilePlayer == null) return;

        var characterVisual = this.ReptilePlayer.characterVisual;
        var prevSpraycanState = this.ReptilePlayer.spraycanState;

        var boostpackEffect = (BoostpackEffectMode) update.Boostpack;
        var frictionEffect = (FrictionEffectMode) update.Friction;
        var spraycan = update.Spraycan;
        var phone = update.Phone;
        var spraycanState = (Reptile.Player.SpraycanState) update.SpraycanState;

        characterVisual.hasEffects = true;
        characterVisual.hasBoostPack = true;

        this.playerManager.SettingVisual = true;
        characterVisual.SetBoostpackEffect(boostpackEffect);
        characterVisual.SetFrictionEffect(frictionEffect);
        characterVisual.SetSpraycan(spraycan);
        characterVisual.SetPhone(phone);

        if (prevSpraycanState != spraycanState) {
            this.ReptilePlayer.SetSpraycanState(spraycanState);
        }

        this.PhoneOut = phone;
        this.playerManager.SettingVisual = false;
    }

    public void HandleAnimationUpdate(AnimationUpdate update) {
        if (this.ReptilePlayer == null) return;

        this.playerManager.PlayingAnimation = true;
        this.ReptilePlayer.PlayAnim(update.Animation, update.ForceOverwrite, update.Instant, update.Time);
        this.playerManager.PlayingAnimation = false;
    }

    public void UpdateIfDifferent(Common.Proto.Player player) {
        if (this.ReptilePlayer == null) return;

        // TODO: account for custom character info
        var differentCharacter = this.SlopPlayer.CharacterInfo.Character != player.CharacterInfo.Character;
        var differentOutfit = this.SlopPlayer.CharacterInfo.Outfit != player.CharacterInfo.Outfit;
        var differentMoveStyle = this.SlopPlayer.CharacterInfo.MoveStyle != player.CharacterInfo.MoveStyle;
        var isDifferent = differentCharacter || differentOutfit || differentMoveStyle;

        if (isDifferent) {
            if (differentOutfit && !differentCharacter) {
                this.ReptilePlayer.SetOutfit(player.CharacterInfo.Outfit);
            } else if (differentCharacter || differentOutfit) {
                // New outfit
                this.ReptilePlayer.SetCharacter(
                    (Characters) player.CharacterInfo.Character,
                    player.CharacterInfo.Outfit
                );
            }

            if (differentMoveStyle) {
                var moveStyle = (MoveStyle) player.CharacterInfo.MoveStyle;
                var equipped = moveStyle != MoveStyle.ON_FOOT;
                this.ReptilePlayer.SetCurrentMoveStyleEquipped(moveStyle);
                this.ReptilePlayer.SwitchToEquippedMovestyle(equipped);
            }

            this.SlopPlayer = player;
        }
    }

    public void QueuePositionUpdate(PositionUpdate newUpdate) {
        if (this.ReptilePlayer == null) return;
        
        this.targetUpdate = newUpdate;
        var latency = newUpdate.Latency / 1000f;
        var timeToMove = this.connectionManager.TickRate!.Value + latency;
        
        var currentPos = this.ReptilePlayer.tf.position;
        var targetPos = ((System.Numerics.Vector3) this.targetUpdate.Transform.Position).ToMentalDeficiency();
        var posDiff = targetPos - currentPos;
        var posVelocity = posDiff / timeToMove;

        var currentRot = this.ReptilePlayer.tf.rotation;
        var targetRot = ((System.Numerics.Quaternion) this.targetUpdate.Transform.Rotation).ToMentalDeficiency();
        var rotDiff = UnityEngine.Quaternion.Angle(currentRot, targetRot);
        var rotVelocity = rotDiff / timeToMove;
        
        this.targetPosSpeed = posVelocity.magnitude;
        this.targetRotSpeed = rotVelocity;
    }

    public void Update() {
        this.ProcessPositionUpdate();
        if (this.MapPin != null) this.MapPin.SetLocation();
    }

    // TODO: this interp code sucks. I don't understand how the previous interp code works anymore
    // but I can't seem to get fluid feeling movement working anymore - help appreciated :D
    public void ProcessPositionUpdate() {
        if (this.targetUpdate is null || this.ReptilePlayer == null) return;

        var latency = this.targetUpdate.Latency / 1000f;
        var timeToMove = this.connectionManager.TickRate!.Value + latency;

        var currentPos = this.ReptilePlayer.tf.position;
        var targetPos = ((System.Numerics.Vector3) this.targetUpdate.Transform.Position).ToMentalDeficiency();
        
        var newPos = UnityEngine.Vector3.MoveTowards(
            currentPos,
            targetPos,
            this.targetPosSpeed!.Value * Time.deltaTime
        );
        this.ReptilePlayer.tf.position = newPos;

        var currentRot = this.ReptilePlayer.tf.rotation;
        var targetRot = ((System.Numerics.Quaternion) this.targetUpdate.Transform.Rotation).ToMentalDeficiency();

        var newRot = UnityEngine.Quaternion.RotateTowards(
            currentRot,
            targetRot,
            this.targetRotSpeed!.Value * Time.deltaTime
        );
        this.ReptilePlayer.tf.rotation = newRot;

        if (this.ReptilePlayer.characterVisual != null) {
            this.ReptilePlayer.characterVisual.transform.rotation = newRot;
        }
    }
}
