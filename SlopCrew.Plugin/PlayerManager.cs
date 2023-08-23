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

namespace SlopCrew.Plugin;

public class PlayerManager : IDisposable {
    public const float ShittyTickRate = 1f / 5f;

    public Player? Guy;
    public int CurrentOutfit = 0;
    public Dictionary<string, AssociatedPlayer> Players = new();
    public List<AssociatedPlayer> AssociatedPlayers => this.Players.Values.ToList();

    private float updateTick = 0;
    private List<NetworkMessage> messageQueue = new();

    public bool IsRefreshQueued = false;
    public bool IsRefreshLocked = false; // JANK

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

    public void SpawnTest() {
        /*var worldHandler = WorldHandler.instance;
        // fucking Unity I swear to god I'm gonna kill your family
        var playerTransform = worldHandler.GetCurrentPlayer().transform;
        var targetTransform = new GameObject("UnityFuckingSucksLmao").transform;

        targetTransform.SetPositionAndRotation(playerTransform.position, playerTransform.rotation);
        //targetTransform.Translate(new Vector3(0, 50, 0));

        this.Guy = worldHandler.SetupAIPlayerAt(
            targetTransform,
            Characters.metalHead,
            PlayerType.NONE
        );*/
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

        var player = worldHandler.SetupAIPlayerAt(
            targetTransform,
            (Characters) slopPlayer.Character,
            PlayerType.NONE,
            outfit: slopPlayer.Outfit,
            moveStyleEquipped: (MoveStyle) slopPlayer.MoveStyle
        );

        var a = slopPlayer.Velocity.ToMentalDeficiency();
        player.SetVelocity(a);

        return player;
    }

    public void Update() {
        var me = WorldHandler.instance?.GetCurrentPlayer();
        if (me is null) return;

        var dt = Time.deltaTime;
        this.updateTick += dt;

        if (this.updateTick > ShittyTickRate) {
            this.updateTick -= ShittyTickRate;
            Plugin.NetworkConnection.SendMessage(new ServerboundPositionUpdate {
                Position = me.transform.position.FromMentalDeficiency(),
                Rotation = me.transform.rotation.FromMentalDeficiency(),
                Velocity = me.motor.velocity.FromMentalDeficiency()
            });
        }

        if (this.messageQueue.Count > 0) {
            var msg = this.messageQueue[0];
            this.messageQueue.RemoveAt(0);
            this.OnMessageInternal(msg);
        }

        if (this.IsRefreshQueued && !this.IsRefreshLocked) {
            this.IsRefreshQueued = false;
            this.RefreshPlayerHello();

            this.IsRefreshLocked = true;
            new Thread(() => {
                Thread.Sleep(5000);
                this.IsRefreshLocked = false;
            }).Start();
        }
    }

    private void OnMessage(NetworkMessage msg) {
        this.messageQueue.Add(msg);
    }

    private void OnMessageInternal(NetworkMessage msg) {
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
                foreach (var player in playersUpdate.Players) {
                    if (!this.Players.ContainsKey(player.Name)) {
                        this.Players.Add(player.Name, new AssociatedPlayer(player));
                    } else {
                        // update player look
                        if (this.Players.TryGetValue(player.Name, out var associatedPlayer)) {
                            associatedPlayer.ResetReptilePlayer(player);
                        }
                    }
                }

                var newPlayers = playersUpdate.Players.Select(x => x.Name).ToList();
                foreach (var currentPlayer in this.Players.Keys) {
                    if (!newPlayers.Contains(currentPlayer)) {
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
                //Name = Guid.NewGuid().ToString(),
                Name = Environment.GetEnvironmentVariable("USERNAME")!,

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
}
