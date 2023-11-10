using System;
using Reptile;
using SlopCrew.Common;
using SlopCrew.Common.Proto;
using UnityEngine;
using Object = UnityEngine.Object;
using Transform = SlopCrew.Common.Proto.Transform;

namespace SlopCrew.Plugin;

public class AssociatedPlayer : IDisposable {
    public Common.Proto.Player SlopPlayer;
    public Reptile.Player ReptilePlayer;

    private SlopConnectionManager connectionManager;
    private PositionUpdate? positionUpdate;

    private UnityEngine.Vector3 velocity = new();

    public AssociatedPlayer(
        SlopConnectionManager connectionManager,
        Common.Proto.Player slopPlayer
    ) {
        this.connectionManager = connectionManager;
        this.SlopPlayer = slopPlayer;

        var emptyGameObject = new GameObject("SlopCrew_EmptyGameObject");
        var emptyTransform = emptyGameObject.transform;

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
        if (this.ReptilePlayer != null) Object.Destroy(this.ReptilePlayer.gameObject);
    }

    public void UpdateIfDifferent(Common.Proto.Player player) {
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

    public void ProcessPositionUpdate(PositionUpdate newUpdate) {
        var lerpTime = this.positionUpdate is null
                           ? 0
                           : (newUpdate.Tick - this.positionUpdate.Tick) / this.connectionManager.TickRate;
        var latency = (newUpdate.Latency + this.connectionManager.Latency) / 1000f / 2f;
        var timeToTarget = lerpTime + latency;
        
        this.positionUpdate = newUpdate;

        System.Numerics.Vector3 targetPos = newUpdate.Transform.Position!;
        System.Numerics.Quaternion targetRot = newUpdate.Transform.Rotation!;
        
        var tf = this.ReptilePlayer.transform;
        var newPos = UnityEngine.Vector3.SmoothDamp(
            tf.position,
            targetPos.ToMentalDeficiency(),
            ref this.velocity,
            timeToTarget!.Value
        );
        this.ReptilePlayer.transform.position = newPos;

        var newRot = UnityEngine.Quaternion.RotateTowards(
            tf.rotation,
            targetRot.ToMentalDeficiency(),
            360f * Time.deltaTime
        );
        this.ReptilePlayer.transform.rotation = newRot;

        if (this.ReptilePlayer.characterVisual != null) {
            this.ReptilePlayer.characterVisual.transform.rotation = newRot;
        }
    }
}
