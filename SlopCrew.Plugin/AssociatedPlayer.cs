using System;
using System.Collections.Generic;
using System.Linq;
using Reptile;
using SlopCrew.Common.Proto;
using SlopCrew.Plugin.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SlopCrew.Plugin;

public class AssociatedPlayer : IDisposable {
    public Common.Proto.Player SlopPlayer;
    public Reptile.Player ReptilePlayer;
    public MapPin? MapPin;

    private PlayerManager playerManager;
    private ConnectionManager connectionManager;
    private Config config;
    private CharacterInfoManager characterInfoManager;

    private UnityEngine.Vector3 velocity = new();
    private PositionUpdate? targetUpdate = null;
    private float? targetPosSpeed = null;
    private float? targetRotSpeed = null;
    private bool doTheFunny;

    public bool PhoneOut = false;

    private const float NameplateHeightFactor = 1.33f;

    // This is bad. I don't know what's better.
    public AssociatedPlayer(
        PlayerManager playerManager,
        ConnectionManager connectionManager,
        Config config,
        CharacterInfoManager characterInfoManager,
        Common.Proto.Player slopPlayer
    ) {
        this.playerManager = playerManager;
        this.connectionManager = connectionManager;
        this.config = config;
        this.characterInfoManager = characterInfoManager;
        this.SlopPlayer = slopPlayer;

        var emptyGameObject = new GameObject("SlopCrew_EmptyGameObject");
        var emptyTransform = emptyGameObject.transform;
        emptyTransform.position = slopPlayer.Transform.Position.NetworkToUnity();
        emptyTransform.rotation = slopPlayer.Transform.Rotation.NetworkToUnity();

        var character = (Characters) slopPlayer.CharacterInfo.Character;
        var outfit = slopPlayer.CharacterInfo.Outfit;
        var moveStyle = (MoveStyle) slopPlayer.CharacterInfo.MoveStyle;

        this.ProcessCharacterInfo(slopPlayer.CustomCharacterInfo.ToList());
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
            this.SpawnNameplate(playerManager.InterfaceUtility);
        }

        if (this.config.General.ShowPlayerMapPins.Value) {
            this.SpawnMapPin();
        }

        // I know you're reading this don't spoil it :(
        this.doTheFunny = DateTime.Now.Month == 4 && DateTime.Now.Day == 1;

        if (this.doTheFunny) {
            var assets = Core.Instance.Assets;
            const string bundle = "city_assets";
            if (!assets.availableBundles.ContainsKey(bundle)) assets.LoadAssetBundleByName(bundle);
            var assetBundle = assets.availableBundles[bundle].AssetBundle;

            var prefab = assetBundle.LoadAsset<GameObject>("Mascot_Polo_street");
            var material = assetBundle.LoadAsset<Material>("MascotAtlas_MAT");

            var polo = Object.Instantiate(prefab, this.ReptilePlayer.tf);
            polo.GetComponent<MeshRenderer>().material = material;
            polo.transform.localRotation = UnityEngine.Quaternion.Euler(-90, 180, 0);

            this.ReptilePlayer.characterVisual.mainRenderer.enabled = false;
        }

        Object.Destroy(emptyGameObject);
    }

    public void ProcessCharacterInfo(List<CustomCharacterInfo> infos)
        => this.characterInfoManager.ProcessCharacterInfo(infos);

    private void SpawnNameplate(InterfaceUtility interfaceUtility) {
        var container = new GameObject("SlopCrew_NameplateContainer");

        // Setup the nameplate itself
        var nameplate = new GameObject("SlopCrew_Nameplate");
        var tmp = nameplate.AddComponent<TextMeshPro>();
        nameplate.AddComponent<TextMeshProFilter>();

        tmp.font = interfaceUtility.NameplateFont;
        tmp.fontMaterial = interfaceUtility.NameplateFontMaterial;
        tmp.alignment = TextAlignmentOptions.Midline;
        tmp.fontSize = 2.5f;
        // Needed to make it appear in front of graffiti decals
        tmp.sortingOrder = 1;
        tmp.SetText(this.SlopPlayer.Name);

        nameplate.transform.parent = container.transform;

        if (this.SlopPlayer.IsCommunityContributor) {
            var devIcon = new GameObject("SlopCrew_DevIcon");
            devIcon.name = "SlopCrew_DevIcon";
            devIcon.SetActive(true);

            var spriteRenderer = devIcon.AddComponent<SpriteRenderer>();

            // Caching this seems to break
            var gameplay = Core.Instance.UIManager.gameplay;
            spriteRenderer.sprite = gameplay.wanted1.GetComponent<Image>().sprite;
            // Needed to make it appear in front of graffiti decals
            spriteRenderer.sortingOrder = 1;

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
        
        if (this.SlopPlayer.RepresentingCrew is { } crew) {
            var crewTag = new GameObject("SlopCrew_CrewTag");
            var crewTagText = crewTag.AddComponent<TextMeshPro>();
            crewTag.AddComponent<TextMeshProFilter>();
            
            crewTagText.font = interfaceUtility.NameplateFont;
            crewTagText.fontMaterial = interfaceUtility.NameplateFontMaterial;
            crewTagText.alignment = TextAlignmentOptions.Midline;
            crewTagText.fontSize = 1.5f;

            crewTagText.sortingOrder = 1;
            crewTagText.SetText(crew);

            crewTag.transform.SetParent(container.transform, false);

            // Needed to populate the bounds
            tmp.ForceMeshUpdate();
            crewTagText.ForceMeshUpdate();
            var nameplateSize = tmp.bounds.size;
            var crewTagSize = crewTagText.bounds.size;
            
            // Offset it to sit above the nameplate, on the top left
            var x = (-nameplateSize.x / 2) + (crewTagSize.x / 2);
            var y = (nameplateSize.y / 2) + (crewTagSize.y / 2);
            crewTag.transform.localPosition = new UnityEngine.Vector3(x, y, 0);
        }

        var capsule = this.ReptilePlayer.interactionCollider as CapsuleCollider;
        // float * float before float * vector is faster because reasons
        container.transform.localPosition = UnityEngine.Vector3.up * (capsule!.height * NameplateHeightFactor);
        container.transform.Rotate(0, 180, 0);
        
        if (this.config.General.BillboardNameplates.Value) {
            container.AddComponent<UIBillboard>();
        }
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
            if (worldHandler is {SceneObjectsRegister.players: not null}) {
                worldHandler.SceneObjectsRegister.players.Remove(this.ReptilePlayer);
            }

            Object.Destroy(this.ReptilePlayer.gameObject);
        }

        if (this.MapPin != null) {
            Object.Destroy(this.MapPin.gameObject);
        }
    }

    public void HandleVisualUpdate(VisualUpdate update) {
        if (this.ReptilePlayer == null)
            return;

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
        if (this.ReptilePlayer == null)
            return;

        this.playerManager.PlayingAnimation = true;
        this.ReptilePlayer.PlayAnim(update.Animation, update.ForceOverwrite, update.Instant, update.Time);
        this.playerManager.PlayingAnimation = false;
    }

    public void UpdateIfDifferent(Common.Proto.Player player) {
        if (this.ReptilePlayer == null)
            return;

        var anyDifferentCharacterInfo = player.CustomCharacterInfo.Count != this.SlopPlayer.CustomCharacterInfo.Count;
        foreach (var info in player.CustomCharacterInfo) {
            var oldInfo = this.SlopPlayer.CustomCharacterInfo.FirstOrDefault(i => i.Id == info.Id);
            if (oldInfo == null || !oldInfo.Data.Equals(info.Data)) {
                anyDifferentCharacterInfo = true;
                break;
            }
        }

        var differentCharacter = this.SlopPlayer.CharacterInfo.Character != player.CharacterInfo.Character
                                 || anyDifferentCharacterInfo;
        var differentOutfit = this.SlopPlayer.CharacterInfo.Outfit != player.CharacterInfo.Outfit;
        var differentMoveStyle = this.SlopPlayer.CharacterInfo.MoveStyle != player.CharacterInfo.MoveStyle;
        var isDifferent = differentCharacter || differentOutfit || differentMoveStyle;

        if (isDifferent) {
            if (differentOutfit && !differentCharacter) {
                this.ReptilePlayer.SetOutfit(player.CharacterInfo.Outfit);
            } else if (differentCharacter || differentOutfit) {
                this.ProcessCharacterInfo(player.CustomCharacterInfo.ToList());
                this.ReptilePlayer.SetCharacter(
                    (Characters) player.CharacterInfo.Character,
                    player.CharacterInfo.Outfit
                );
                this.ReptilePlayer.InitVisual();
                if (this.doTheFunny) this.ReptilePlayer.characterVisual.mainRenderer.enabled = false;
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
        if (this.ReptilePlayer == null)
            return;

        this.targetUpdate = newUpdate;
        var latency = newUpdate.Latency / 1000f;
        var timeToMove = this.connectionManager.TickRate!.Value + latency;

        var currentPos = this.ReptilePlayer.tf.position;
        var targetPos = this.targetUpdate.Transform.Position.NetworkToUnity();
        var posDiff = targetPos - currentPos;
        var posVelocity = posDiff / timeToMove;

        var currentRot = this.ReptilePlayer.tf.rotation;
        var targetRot = this.targetUpdate.Transform.Rotation.NetworkToUnity();
        var rotDiff = UnityEngine.Quaternion.Angle(currentRot, targetRot);
        var rotVelocity = rotDiff / timeToMove;

        this.targetPosSpeed = posVelocity.magnitude;
        this.targetRotSpeed = rotVelocity;
    }

    public void Update() {
        this.ProcessPositionUpdate();
        if (this.MapPin != null)
            this.MapPin.SetLocation();
    }

    // TODO: this interp code sucks. I don't understand how the previous interp code works anymore
    // but I can't seem to get fluid feeling movement working anymore - help appreciated :D
    public void ProcessPositionUpdate() {
        if (this.targetUpdate is null || this.ReptilePlayer == null)
            return;

        var latency = this.targetUpdate.Latency / 1000f;
        var timeToMove = this.connectionManager.TickRate!.Value + latency;

        var currentPos = this.ReptilePlayer.tf.position;
        var targetPos = this.targetUpdate.Transform.Position.NetworkToUnity();

        var newPos = UnityEngine.Vector3.MoveTowards(
            currentPos,
            targetPos,
            this.targetPosSpeed!.Value * Time.deltaTime
        );
        this.ReptilePlayer.tf.position = newPos;

        var currentRot = this.ReptilePlayer.tf.rotation;
        var targetRot = this.targetUpdate.Transform.Rotation.NetworkToUnity();

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
