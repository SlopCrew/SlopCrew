using Reptile;
using UnityEngine;
using DG.Tweening;
using SlopCrew.Common.Network.Clientbound;
using Player = SlopCrew.Common.Player;

namespace SlopCrew.Plugin;

public class AssociatedPlayer {
    public Common.Player SlopPlayer;
    public Reptile.Player ReptilePlayer;

    private Vector3 tweenPos;

    public AssociatedPlayer(Common.Player slopPlayer) {
        this.SlopPlayer = slopPlayer;
        this.ReptilePlayer = PlayerManager.SpawnReptilePlayer(slopPlayer);
    }

    public void FuckingObliterate() {
        WorldHandler.instance.SceneObjectsRegister.players.Remove(this.ReptilePlayer);

        if (this.ReptilePlayer.gameObject is not null)
            Object.Destroy(this.ReptilePlayer.gameObject);

        if (this.ReptilePlayer is not null)
            Object.Destroy(this.ReptilePlayer);
    }

    public void ResetReptilePlayer(Common.Player slopPlayer) {
        this.SlopPlayer = slopPlayer;
        this.FuckingObliterate();
        this.ReptilePlayer = PlayerManager.SpawnReptilePlayer(slopPlayer);
    }

    public void SetPos(ClientboundPlayerPositionUpdate posUpdate) {
        if (this.ReptilePlayer is not null && this.ReptilePlayer.isActiveAndEnabled) {
            var sequence = DOTween.Sequence();

            var posTween = DOTween.To(
                () => this.ReptilePlayer.tf.position,
                x => this.ReptilePlayer.tf.position = x,
                posUpdate.Position.ToMentalDeficiency(),
                PlayerManager.ShittyTickRate
            );

            var rotTween = DOTween.To(
                () => this.ReptilePlayer.tf.rotation,
                x => this.ReptilePlayer.tf.rotation = x,
                posUpdate.Rotation.ToMentalDeficiency().eulerAngles,
                PlayerManager.ShittyTickRate
            );

            sequence.Append(posTween).Append(rotTween);
            sequence.OnComplete(() => {
                // lol
            });
        }
    }
}
