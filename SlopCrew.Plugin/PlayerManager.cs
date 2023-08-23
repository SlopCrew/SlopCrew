using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using HarmonyLib;
using Reptile;
using SlopCrew.Common.Network;
using SlopCrew.Common.Network.Clientbound;
using SlopCrew.Common.Network.Serverbound;
using UnityEngine;
using Vector3 = System.Numerics.Vector3;

namespace SlopCrew.Plugin;

public class PlayerManager : IDisposable {
    public const float ShittyTickRate = 1f / 24f;

    public int CurrentOutfit = 0;
    public Dictionary<uint, AssociatedPlayer> Players = new();
    public List<AssociatedPlayer> AssociatedPlayers => this.Players.Values.ToList();

    private float updateTick = 0;
    private List<NetworkSerializable> messageQueue = new();

    public bool IsRefreshQueued = false;

    private int? lastAnimation;
    private Vector3 lastPos = Vector3.Zero;

    public PlayerManager() {
        StageManager.OnStagePostInitialization += this.StageInit;
        Plugin.NetworkConnection.OnMessageReceived += this.OnMessage;
    }

    public void Dispose() {
        StageManager.OnStagePostInitialization -= this.StageInit;
        Plugin.NetworkConnection.OnMessageReceived -= this.OnMessage;
    }

    public AssociatedPlayer? GetAssociatedPlayer(Reptile.Player reptilePlayer) {
        return this.AssociatedPlayers.FirstOrDefault(x => x.ReptilePlayer == reptilePlayer);
    }

    private void StageInit() {
        this.IsRefreshQueued = true;
    }

    public static Reptile.Player SpawnReptilePlayer(Common.Player slopPlayer) {
        var worldHandler = WorldHandler.instance;

        // Why the hell is this the only way to get an empty transform
        var targetTransform = new GameObject("UnitySucksLmao").transform;
        targetTransform.SetPositionAndRotation(slopPlayer.Position.ToMentalDeficiency(),
                                               slopPlayer.Rotation.ToMentalDeficiency());

        Plugin.Log.LogInfo("Creating player for " + slopPlayer.Name + " at " + slopPlayer.Position);
        var player = worldHandler.SetupAIPlayerAt(
            targetTransform,
            (Characters) slopPlayer.Character,
            PlayerType.NONE,
            outfit: slopPlayer.Outfit,
            moveStyleEquipped: (MoveStyle) slopPlayer.MoveStyle
        );

        //var vel = slopPlayer.Velocity.ToMentalDeficiency();
        //player.SetVelocity(vel);

        return player;
    }

    public void Update() {
        var me = WorldHandler.instance?.GetCurrentPlayer();
        if (me is null) return;

        var dt = Time.deltaTime;
        this.updateTick += dt;

        if (this.updateTick > ShittyTickRate) {
            this.updateTick -= ShittyTickRate;

            var deltaMove = me.transform.position.FromMentalDeficiency() - this.lastPos;
            var moved = Math.Abs(deltaMove.Length()) > 0.125;
            if (moved) {
                var position = me.transform.position;
                this.lastPos = position.FromMentalDeficiency();

                Plugin.NetworkConnection.SendMessage(new ServerboundPositionUpdate {
                    Position = position.FromMentalDeficiency(),
                    Rotation = me.transform.rotation.FromMentalDeficiency(),
                    Velocity = me.motor.velocity.FromMentalDeficiency()
                });
            }
        }

        if (this.messageQueue.Count > 0) {
            var msg = this.messageQueue[0];
            this.messageQueue.RemoveAt(0);
            this.OnMessageInternal(msg);
        }

        // FIXME lmao this sucks
        if (this.IsRefreshQueued) {
            this.IsRefreshQueued = false;
            this.RefreshPlayerHello();
        }
    }

    private void OnMessage(NetworkSerializable msg) {
        this.messageQueue.Add(msg);
    }

    private void OnMessageInternal(NetworkSerializable msg) {
        switch (msg) {
            case ClientboundPlayerAnimation playerAnimation: {
                if (this.Players.TryGetValue(playerAnimation.Player, out var associatedPlayer)) {
                    if (associatedPlayer.ReptilePlayer is not null) {
                        associatedPlayer.ReptilePlayer.PlayAnim(
                            playerAnimation.Animation,
                            playerAnimation.ForceOverwrite,
                            playerAnimation.Instant,
                            playerAnimation.AtTime
                        );
                    }
                }
                break;
            }

            case ClientboundPlayersUpdate playersUpdate: {
                Plugin.Log.LogInfo("ClientboundPlayersUpdate len: " + playersUpdate.Players.Count);
                foreach (var player in playersUpdate.Players) {
                    Plugin.Log.LogInfo("ClientboundPlayersUpdate player: " + player.Name + ", id: " + player.ID);

                    if (!this.Players.ContainsKey(player.ID)) {
                        Plugin.Log.LogInfo("ClientboundPlayersUpdate Spawning AssociatedPlayer");
                        this.Players.Add(player.ID, new AssociatedPlayer(player));
                    } else {
                        // update player look
                        if (this.Players.TryGetValue(player.ID, out var associatedPlayer)) {
                            Plugin.Log.LogInfo("Updating associated player look");
                            associatedPlayer.ResetReptilePlayer(player);
                        }
                    }
                }

                var currentPlayers = this.Players.Keys.ToList();
                var newPlayers = playersUpdate.Players.Select(x => x.ID).ToList();
                foreach (var currentPlayer in currentPlayers) {
                    if (!newPlayers.Contains(currentPlayer)) {
                        Plugin.Log.LogInfo("ClientboundPlayersUpdate Removing AssociatedPlayer " + currentPlayer);
                        if (this.Players.TryGetValue(currentPlayer, out var associatedPlayer)) {
                            associatedPlayer.FuckingObliterate();
                            this.Players.Remove(currentPlayer);
                        }
                    }
                }
                break;
            }

            case ClientboundPlayerPositionUpdate playerPositionUpdate: {
                if (this.Players.TryGetValue(playerPositionUpdate.Player, out var associatedPlayer)) {
                    associatedPlayer.SetPos(playerPositionUpdate);
                }

                break;
            }
        }
    }

    public void RefreshPlayerHello() {
        var me = WorldHandler.instance?.GetCurrentPlayer();
        if (me is null) {
            // lol
            this.IsRefreshQueued = true;
            return;
        }

        var traverse = Traverse.Create(me);
        var character = traverse.Field<Characters>("character").Value;
        var moveStyle = traverse.Field<MoveStyle>("moveStyle").Value;

        Plugin.NetworkConnection.SendMessage(new ServerboundPlayerHello {
            Player = new() {
                Name = Plugin.ConfigUsername.Value,
                ID = 1337,

                Stage = (int) Core.Instance.BaseModule.CurrentStage,
                Character = (int) character,
                Outfit = this.CurrentOutfit,
                MoveStyle = (int) moveStyle,

                Position = me.transform.position.FromMentalDeficiency(),
                Rotation = me.transform.rotation.FromMentalDeficiency(),
                Velocity = me.motor.velocity.FromMentalDeficiency()
            }
        });
    }

    public void PlayAnimation(int anim, bool forceOverwrite, bool instant, float atTime) {
        // Sometimes the game likes to spam animations. Why? idk lol
        if (this.lastAnimation == anim) return;
        this.lastAnimation = anim;

        Plugin.NetworkConnection.SendMessage(new ServerboundAnimation {
            Animation = anim,
            ForceOverwrite = forceOverwrite,
            Instant = instant,
            AtTime = atTime
        });
    }
}
