using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using HarmonyLib;
using Reptile;
using SlopCrew.Common;
using SlopCrew.Common.Network;
using SlopCrew.Common.Network.Clientbound;
using SlopCrew.Common.Network.Serverbound;
using UnityEngine;
using Vector3 = System.Numerics.Vector3;

namespace SlopCrew.Plugin;

public class PlayerManager : IDisposable {
    public int CurrentOutfit = 0;
    public bool IsHelloRefreshQueued = false;
    public bool IsVisualRefreshQueued = false;
    public bool IsResetQueued = false;

    public bool IsPlayingAnimation = false;
    public bool IsSettingVisual = false;

    public Dictionary<uint, AssociatedPlayer> Players = new();
    public List<AssociatedPlayer> AssociatedPlayers => this.Players.Values.ToList();

    private Queue<NetworkSerializable> messageQueue = new();
    public static uint ServerTick = 0;
    private float updateTick = 0;
    private int? lastAnimation;
    private Vector3 lastPos = Vector3.Zero;
   

    public PlayerManager() {
        Core.OnUpdate += this.Update;
        StageManager.OnStageInitialized += this.StageInit;
        StageManager.OnStagePostInitialization += this.StagePostInit;
        Plugin.NetworkConnection.OnMessageReceived += this.OnMessage;

        new Thread(() => {
            const int tickRate = (int) (Constants.TickRate * 1000);
            while (true) {
                Thread.Sleep(tickRate);
                ServerTick++;
            }
        }).Start();
    }

    public void Reset() {
        this.CurrentOutfit = 0;
        this.IsHelloRefreshQueued = false;
        this.IsVisualRefreshQueued = false;
        this.IsResetQueued = false;
        this.IsPlayingAnimation = false;
        this.IsSettingVisual = false;

        this.Players.Values.ToList().ForEach(x => x.FuckingObliterate());
        this.Players.Clear();
        this.messageQueue.Clear();

        this.updateTick = 0;
        this.lastAnimation = null;
        this.lastPos = Vector3.Zero;
    }

    public void Dispose() {
        Core.OnUpdate -= this.Update;
        StageManager.OnStageInitialized -= this.StageInit;
        StageManager.OnStagePostInitialization -= this.StagePostInit;
        Plugin.NetworkConnection.OnMessageReceived -= this.OnMessage;
    }

    public AssociatedPlayer? GetAssociatedPlayer(Reptile.Player reptilePlayer) {
        return this.AssociatedPlayers.FirstOrDefault(x => x.ReptilePlayer == reptilePlayer);
    }

    private void StageInit() {
        this.Players.Clear();
    }

    private void StagePostInit() {
        this.IsHelloRefreshQueued = true;
    }

    public static Reptile.Player SpawnReptilePlayer(Common.Player slopPlayer) {
        var worldHandler = WorldHandler.instance;

        // Why the hell is this the only way to get an empty transform
        var targetTransform = new GameObject("UnitySucksLmao").transform;
        targetTransform.SetPositionAndRotation(slopPlayer.Position.ToMentalDeficiency(),
                                               slopPlayer.Rotation.ToMentalDeficiency());

        //Plugin.Log.LogInfo("Creating player for " + slopPlayer.Name + " at " + slopPlayer.Position);
        var player = worldHandler.SetupAIPlayerAt(
            targetTransform,
            (Characters) slopPlayer.Character,
            PlayerType.NONE,
            outfit: slopPlayer.Outfit,
            moveStyleEquipped: (MoveStyle) slopPlayer.MoveStyle
        );

        player.motor.gravity = 0;

        //var vel = slopPlayer.Velocity.ToMentalDeficiency();
        //player.SetVelocity(vel);

        return player;
    }

    public void Update() {      
        if (this.IsResetQueued) {
            this.IsResetQueued = false;
            this.Reset();
            return;
        }

        var me = WorldHandler.instance?.GetCurrentPlayer();
        if (me is null) return;
        var traverse = Traverse.Create(me);
        
        var dt = Time.deltaTime;
        this.updateTick += dt;

        if (this.updateTick <= Constants.TickRate) return;

        this.updateTick = 0;       

        HandlePositionUpdate(me);

        ProcessMessageQueue();

        HandleHelloRefresh(me, traverse);

        HandleVisualRefresh(me, traverse);

        UpdatePlayerCount();
    }

    private void HandlePositionUpdate(Reptile.Player me) {
        var position = me.transform.position;
        var deltaMove = position.FromMentalDeficiency() - this.lastPos;
        var moved = Math.Abs(deltaMove.Length()) > 0.125;

        if (moved) {
            this.lastPos = position.FromMentalDeficiency();

            Plugin.NetworkConnection.SendMessage(new ServerboundPositionUpdate {
                Position = position.FromMentalDeficiency(),
                Rotation = me.transform.rotation.FromMentalDeficiency(),
                Velocity = me.motor.velocity.FromMentalDeficiency()
            });
        }
    }

    private void ProcessMessageQueue() {
        while (this.messageQueue.Count > 0) {
            var msg = this.messageQueue.Dequeue();
            this.OnMessageInternal(msg);
        }
    }

    private void HandleHelloRefresh(Reptile.Player me, Traverse traverse) {
        if (!this.IsHelloRefreshQueued) return;

        this.IsHelloRefreshQueued = false;
        var character = traverse.Field<Characters>("character").Value;
        var moveStyle = traverse.Field<MoveStyle>("moveStyle").Value;

        Plugin.NetworkConnection.SendMessage(new ServerboundPlayerHello {
            Player = new() {
                Name = Plugin.ConfigUsername.Value,
                ID = 1337, // filled in by the server; could be an int instead of uint but i'd have to change types everywhere

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
    private void HandleVisualRefresh(Reptile.Player me, Traverse traverse) {
        if (!this.IsVisualRefreshQueued) return;

        this.IsVisualRefreshQueued = false;
        var characterVisual = traverse.Field<CharacterVisual>("characterVisual").Value;

        Plugin.NetworkConnection.SendMessage(new ServerboundVisualUpdate {
            BoostpackEffect = (int) characterVisual.boostpackEffectMode,
            FrictionEffect = (int) characterVisual.frictionEffectMode,
            Spraycan = characterVisual.VFX.spraycan.activeSelf
        });
    }

    private void UpdatePlayerCount() {
        Plugin.PlayerCount = this.Players.Count + 1;  // +1 to include the current player
    }

    private void OnMessage(NetworkSerializable msg) {
        this.messageQueue.Enqueue(msg);
    }

    private void OnMessageInternal(NetworkSerializable msg) {
        switch (msg) {
            case ClientboundPlayerAnimation playerAnimation:
                HandlePlayerAnimation(playerAnimation);
                break;

            case ClientboundPlayersUpdate playersUpdate:
                HandlePlayersUpdate(playersUpdate);
                break;

            case ClientboundPlayerPositionUpdate playerPositionUpdate:
                HandlePlayerPositionUpdate(playerPositionUpdate);
                break;

            case ClientboundPlayerVisualUpdate playerVisualUpdate:
                HandlePlayerVisualUpdate(playerVisualUpdate);
                break;

            case ClientBoundSync serverTickUpdate:
                HandleServerTickUpdate(serverTickUpdate);
                break;
        }
    }

    private void HandlePlayerAnimation(ClientboundPlayerAnimation playerAnimation) {
        if (this.Players.TryGetValue(playerAnimation.Player, out var associatedPlayer) && associatedPlayer.ReptilePlayer is not null) {
            this.IsPlayingAnimation = true;
            associatedPlayer.ReptilePlayer.PlayAnim(
                playerAnimation.Animation,
                playerAnimation.ForceOverwrite,
                playerAnimation.Instant,
                playerAnimation.AtTime
            );
            this.IsPlayingAnimation = false;
        }
    }

    private void HandlePlayersUpdate(ClientboundPlayersUpdate playersUpdate) {
        var newPlayerIds = new HashSet<uint>(playersUpdate.Players.Select(p => p.ID));
        
        foreach (var player in playersUpdate.Players) {
            if (!this.Players.TryGetValue(player.ID, out var associatedPlayer)) {
                // New player
                this.Players[player.ID] = new AssociatedPlayer(player);
            } else {
                UpdateAssociatedPlayerIfDifferent(associatedPlayer, player);
            }
        }
             
        foreach (var currentPlayerId in this.Players.Keys.ToList()) {
            if (!newPlayerIds.Contains(currentPlayerId) && this.Players.TryGetValue(currentPlayerId, out var associatedPlayer)) {
                associatedPlayer.FuckingObliterate();
                this.Players.Remove(currentPlayerId);
            }
        }
    }

    private void UpdateAssociatedPlayerIfDifferent(AssociatedPlayer associatedPlayer, Common.Player player) {
        var oldPlayer = associatedPlayer.SlopPlayer;
        var reptilePlayer = associatedPlayer.ReptilePlayer;

        // TODO: this kinda sucks
        var differentCharacter = oldPlayer.Character != player.Character;
        var differentOutfit = oldPlayer.Outfit != player.Outfit;
        var differentMoveStyle = oldPlayer.MoveStyle != player.MoveStyle;
        var isDifferent = differentCharacter || differentOutfit || differentMoveStyle;

        if (isDifferent) {
            //Plugin.Log.LogInfo("Updating associated player look");

            if (differentOutfit && !differentCharacter) {
                // Outfit-only requires a separate method
                reptilePlayer.SetOutfit(player.Outfit);
            } else if (differentCharacter || differentOutfit) {
                // New outfit
                reptilePlayer.SetCharacter((Characters) player.Character, player.Outfit);
            }

            if (differentMoveStyle) {
                var moveStyle = (MoveStyle) player.MoveStyle;
                var equipped = moveStyle != MoveStyle.ON_FOOT;
                reptilePlayer.SetCurrentMoveStyleEquipped(moveStyle);
                reptilePlayer.SwitchToEquippedMovestyle(equipped);
            }

            associatedPlayer.ResetPlayer(player);
        } else {
            //Plugin.Log.LogInfo("Ignoring associated player look update, no changes");
        }
    }

    private void HandlePlayerPositionUpdate(ClientboundPlayerPositionUpdate playerPositionUpdate) {
        if (this.Players.TryGetValue(playerPositionUpdate.Player, out var associatedPlayer)) {
            associatedPlayer.SetPos(playerPositionUpdate);
        }
    }

    private void HandlePlayerVisualUpdate(ClientboundPlayerVisualUpdate playerVisualUpdate) {
        if (this.Players.TryGetValue(playerVisualUpdate.Player, out var associatedPlayer)) {
            var reptilePlayer = associatedPlayer.ReptilePlayer;
            var traverse = Traverse.Create(reptilePlayer);
            var characterVisual = traverse.Field<CharacterVisual>("characterVisual").Value;

            var boostpackEffect = (BoostpackEffectMode) playerVisualUpdate.BoostpackEffect;
            var frictionEffect = (FrictionEffectMode) playerVisualUpdate.FrictionEffect;
            var spraycan = playerVisualUpdate.Spraycan;

            characterVisual.hasEffects = true;
            characterVisual.hasBoostPack = true;

            // TODO scale
            this.IsSettingVisual = true;
            characterVisual.SetBoostpackEffect(boostpackEffect);
            characterVisual.SetFrictionEffect(frictionEffect);
            characterVisual.SetSpraycan(spraycan);
            this.IsSettingVisual = false;
        }
    }

    private void HandleServerTickUpdate(ClientBoundSync serverTickUpdate) {
        var prevTick = ServerTick;
        ServerTick = serverTickUpdate.ServerTickActual;

        Plugin.Log.LogInfo("SYNCING TICK " + prevTick + " -> " + ServerTick);
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
