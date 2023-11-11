using System;
using Reptile;
using SlopCrew.Common.Proto;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SlopCrew.Plugin;

public class AssociatedPlayer : IDisposable {
    public Common.Proto.Player SlopPlayer;
    public Reptile.Player ReptilePlayer;

    private PlayerManager playerManager;
    private ConnectionManager connectionManager;

    private UnityEngine.Vector3 velocity = new();
    private PositionUpdate? targetUpdate = null;

    public bool PhoneOut = false;

    // This is bad. I don't know what's better.
    public AssociatedPlayer(
        PlayerManager playerManager,
        ConnectionManager connectionManager,
        Common.Proto.Player slopPlayer
    ) {
        this.playerManager = playerManager;
        this.connectionManager = connectionManager;
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
    }

    public void Dispose() {
        if (this.ReptilePlayer != null) {
            WorldHandler.instance.SceneObjectsRegister.players.Remove(this.ReptilePlayer);
            Object.Destroy(this.ReptilePlayer.gameObject);
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
        this.targetUpdate = newUpdate;
    }

    public void Update() {
        this.ProcessPositionUpdate();
    }

    // TODO: this interp code sucks. I don't understand how the previous interp code works anymore
    // but I can't seem to get fluid feeling movement working anymore
    // help appreciated :D
    public void ProcessPositionUpdate() {
        if (this.targetUpdate is null || this.ReptilePlayer == null) return;

        var tickDiff = this.targetUpdate.Tick - this.connectionManager.ServerTick;
        var lerpTime = tickDiff * this.connectionManager.TickRate;

        var latency = (this.targetUpdate.Latency + this.connectionManager.Latency) / 1000f / 2f;
        var timeToTarget = lerpTime + latency;
        if (timeToTarget < 0) timeToTarget = 0;

        var targetPos = ((System.Numerics.Vector3) this.targetUpdate.Transform.Position).ToMentalDeficiency();
        var newPos = UnityEngine.Vector3.SmoothDamp(
            this.ReptilePlayer.transform.position,
            targetPos,
            ref this.velocity,
            timeToTarget!.Value
        );
        this.ReptilePlayer.transform.position = newPos;

        var targetRot = ((System.Numerics.Quaternion) this.targetUpdate.Transform.Rotation).ToMentalDeficiency();
        var newRot = UnityEngine.Quaternion.Slerp(
            this.ReptilePlayer.transform.rotation,
            targetRot,
            timeToTarget.Value
        );
        this.ReptilePlayer.transform.rotation = newRot;

        if (this.ReptilePlayer.characterVisual != null) {
            this.ReptilePlayer.characterVisual.transform.rotation = newRot;
        }
    }
}
