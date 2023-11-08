using EmbedIO.WebSockets;
using Serilog;
using SlopCrew.Common;
using SlopCrew.Common.Encounters;
using SlopCrew.Common.Network;
using SlopCrew.Common.Network.Clientbound;
using SlopCrew.Common.Network.Serverbound;
using SlopCrew.Server.Race;
using System.Security.Cryptography;
using System.Text;

namespace SlopCrew.Server;

public class ConnectionState {
    public Player? Player;
    public int? LastStage = null;
    public int DisconnectTicks = 0;

    public ClientboundPlayerAnimation? QueuedAnimation;
    public Transform? QueuedPositionUpdate;
    public ClientboundPlayerScoreUpdate? QueuedScoreUpdate;
    public ClientboundPlayerVisualUpdate? QueuedVisualUpdate;

    public ClientboundPlayerScoreUpdate? LastScoreUpdate;

    public IWebSocketContext Context;
    public object SendLock;

    public Dictionary<EncounterType, List<uint>> EncounterRequests = new();

    public ConnectionState(IWebSocketContext context) {
        this.Context = context;
        this.SendLock = new();
    }

    public void HandlePacket(NetworkPacket msg) {
        var server = Server.Instance;

        // These packets get processed when player is null
        switch (msg) {
            case ServerboundVersion version: {
                if (version.Version != Constants.NetworkVersion) {
                    this.Context.WebSocket.CloseAsync();
                    Log.Verbose("Connected mod version {Version} does not match server version {NetworkVersion}",
                                version.Version, Constants.NetworkVersion);
                }
                
                server.Module.SendToContext(this.Context, new ClientboundServerConfig {
                    BannedMods = server.Config.Encounters.BannedMods
                });
                return;
            }

            case ServerboundPing ping:
                HandlePing(ping);
                return;

            case ServerboundPlayerHello enter:
                this.HandleHello(enter, server);
                return;
        }

        if (this.Player is null) {
            Log.Verbose("Received message from {DebugName} without a hello, ignoring", this.DebugName());
            return;
        }

        switch (msg) {
            case ServerboundAnimation animation:
                this.HandleAnimation(animation);
                break;

            case ServerboundPositionUpdate positionUpdate:
                this.HandlePositionUpdate(positionUpdate);
                break;

            case ServerboundScoreUpdate scoreUpdate:
                this.HandleScoreUpdate(scoreUpdate);
                break;

            case ServerboundVisualUpdate visualUpdate:
                this.HandleVisualUpdate(visualUpdate);
                break;

            case ServerboundEncounterRequest encounterRequest:
                lock (Server.Instance.Module.Connections) {
                    this.HandleEncounterRequest(encounterRequest);
                }
                break;

            case ServerboundRaceFinish raceFinish:
                this.HandleRaceFinish(raceFinish);
                break;

            case ServerboundEncounterCancel encounterCancel:
                lock (Server.Instance.Module.Connections) {
                    this.HandleEncounterCancel(encounterCancel);
                }
                break;
        }
    }

    private void HandlePing(ServerboundPing ping) {
        Server.Instance.Module.SendToContext(this.Context, new ClientboundPong {
            ID = ping.ID
        });
        this.DisconnectTicks = 0;
    }

    private void HandleHello(ServerboundPlayerHello enter, Server server) {
        // Temporary solution to CharacterAPI players crashing other players
        if (enter.Player.Character is < 0 or > 25)
            enter.Player.Character = 3;

        if (enter.Player.Outfit is < 0 or > 3)
            enter.Player.Outfit = 0;

        if (enter.Player.MoveStyle is < 0 or > 4)
            enter.Player.MoveStyle = 0;

        // SPECIAL_SKATEBOARD causes issues so stop it from syncing ~Sylvie
        // It's also blocked client-side
        if (enter.Player.MoveStyle == 4) // SPECIAL_SKATEBOARD
            enter.Player.MoveStyle = 2;  // SKATEBOARD

        // Assign a unique ID on first hello
        // Subsequent hellos keep the originally assigned ID
        enter.Player.ID = this.Player?.ID ?? server.GetNextID();
        this.Player = enter.Player;

        // Thanks
        this.Player.Name = PlayerNameFilter.DoFilter(this.Player.Name);

        var hash = SHA256.Create();
        var hashBytes = hash.ComputeHash(Encoding.UTF8.GetBytes(enter.SecretCode));
        var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        this.Player.IsDeveloper = Constants.SecretCodes.Contains(hashString);

        // Someone will do it eventually
        if (this.Player.CharacterInfo is {Data.Length: > 64}) {
            this.Player.CharacterInfo.Data = this.Player.CharacterInfo.Data[..64];
        }

        // Syncs player to other players
        lock (Server.Instance.Module.Connections) {
            server.TrackConnection(this);
        }

        // Set after we track connection because State:tm:
        this.LastStage = this.Player.Stage;
    }

    private void HandleAnimation(ServerboundAnimation animation) {
        this.QueuedAnimation = new ClientboundPlayerAnimation {
            Player = this.Player!.ID,
            Animation = animation.Animation,
            ForceOverwrite = animation.ForceOverwrite,
            Instant = animation.Instant,
            AtTime = animation.AtTime
        };
    }

    private void HandlePositionUpdate(ServerboundPositionUpdate positionUpdate) {
        for (var i = 0; i < 3; i++) {
            // check to see if packet will crash clients
            if (!float.IsFinite(positionUpdate.Transform.Position[i])
                || !float.IsFinite(positionUpdate.Transform.Velocity[i])) break;
        }

        this.Player!.Transform = positionUpdate.Transform;

        this.QueuedPositionUpdate = positionUpdate.Transform;
        this.QueuedPositionUpdate.Tick = Server.CurrentTick;
    }

    private void HandleScoreUpdate(ServerboundScoreUpdate scoreUpdate) {
        this.QueuedScoreUpdate = new ClientboundPlayerScoreUpdate {
            Player = this.Player!.ID,
            Score = scoreUpdate.Score,
            BaseScore = scoreUpdate.BaseScore,
            Multiplier = scoreUpdate.Multiplier
        };
    }

    private void HandleVisualUpdate(ServerboundVisualUpdate visualUpdate) {
        this.QueuedVisualUpdate = new ClientboundPlayerVisualUpdate {
            Player = this.Player!.ID,
            BoostpackEffect = visualUpdate.BoostpackEffect,
            FrictionEffect = visualUpdate.FrictionEffect,
            Spraycan = visualUpdate.Spraycan,
            Phone = visualUpdate.Phone,
            SpraycanState = visualUpdate.SpraycanState
        };
    }

    private void HandleEncounterRequest(ServerboundEncounterRequest encounterRequest) {
        if (encounterRequest.EncounterType.IsStateful()) {
            Server.Instance.StatefulEncounterManager.HandleEncounterRequest(this, encounterRequest.EncounterType);
        } else {
            this.HandleStatelessEncounterRequest(encounterRequest);
        }
    }

    private void HandleEncounterCancel(ServerboundEncounterCancel encounterCancel) {
        if (encounterCancel.EncounterType.IsStateful()) {
            if (Server.Instance.StatefulEncounterManager.HandleEncounterCancel(this, encounterCancel.EncounterType)) {
                Server.Instance.Module.SendToContext(this.Context, new ClientboundEncounterCancel {
                    EncounterType = encounterCancel.EncounterType
                });
            }
        }

        // Do we need to handle stateless encounter cancellation?
    }

    private void HandleStatelessEncounterRequest(ServerboundEncounterRequest encounterRequest) {
        var module = Server.Instance.Module;
        if (encounterRequest.PlayerID == this.Player!.ID) return;

        var otherPlayer = Server.Instance.GetConnections()
            .FirstOrDefault(x => x.Player?.ID == encounterRequest.PlayerID);

        if (otherPlayer is null) return;
        if (otherPlayer.Player?.Stage != this.Player.Stage) return;

        Log.Information("{Player} wants to encounter {OtherPlayer}", this.DebugName(), otherPlayer.DebugName());

        if (!otherPlayer.EncounterRequests.ContainsKey(encounterRequest.EncounterType))
            otherPlayer.EncounterRequests[encounterRequest.EncounterType] = new();
        if (!this.EncounterRequests.ContainsKey(encounterRequest.EncounterType))
            this.EncounterRequests[encounterRequest.EncounterType] = new();

        var canSendNotif = false;
        if (!otherPlayer.EncounterRequests[encounterRequest.EncounterType].Contains(otherPlayer.Player.ID)) {
            otherPlayer.EncounterRequests[encounterRequest.EncounterType].Add(this.Player!.ID);
            // Only let people send it once every 5s
            canSendNotif = true;
        }

        if (this.EncounterRequests[encounterRequest.EncounterType].Contains(otherPlayer.Player.ID)) {
            Log.Information("Starting encounter: {Player} vs {OtherPlayer}", this.DebugName(),
                            otherPlayer.DebugName());

            var encounterConfig = Server.Instance.Config.Encounters;
            var length = encounterRequest.EncounterType switch {
                EncounterType.ScoreEncounter => encounterConfig.ScoreDuration,
                EncounterType.ComboEncounter => encounterConfig.ComboDuration,
                _ => 90
            };

            module.SendToContext(this.Context, new ClientboundEncounterStart {
                EncounterType = encounterRequest.EncounterType,
                EncounterConfigData = new SimpleEncounterConfigData {
                    EncounterLength = length,
                    Guid = Guid.Empty,
                    Opponent = otherPlayer.Player.ID
                }
            });

            module.SendToContext(otherPlayer.Context, new ClientboundEncounterStart {
                EncounterType = encounterRequest.EncounterType,
                EncounterConfigData = new SimpleEncounterConfigData {
                    EncounterLength = length,
                    Guid = Guid.Empty,
                    Opponent = this.Player.ID
                }
            });

            this.EncounterRequests[encounterRequest.EncounterType].Remove(otherPlayer.Player.ID);
            otherPlayer.EncounterRequests[encounterRequest.EncounterType].Remove(this.Player.ID);
        } else if (canSendNotif) {
            module.SendToContext(otherPlayer.Context, new ClientboundEncounterRequest {
                PlayerID = this.Player.ID,
                EncounterType = encounterRequest.EncounterType
            });
        }

        Task.Run(async () => {
            await Task.Delay(5000);
            this.EncounterRequests[encounterRequest.EncounterType].Remove(otherPlayer.Player.ID);
            otherPlayer.EncounterRequests[encounterRequest.EncounterType].Remove(this.Player.ID);
        });
    }

    private void HandleRaceFinish(ServerboundRaceFinish raceFinish) {
        lock (Server.Instance.StatefulEncounterManager.Encounters) {
            var encounters = Server.Instance.StatefulEncounterManager.Encounters;
            if (encounters.FirstOrDefault(x => x.EncounterId == raceFinish.Guid) is not RaceStatefulEncounter race) {
                Log.Warning("Received null race for finish {Guid}", raceFinish.Guid);
                return;
            }

            race.AddPlayerTime(this.Player!.ID, raceFinish.Time);
        }
    }

    public string DebugName() {
        var endpoint = this.Context.Headers.Get("X-Forwarded-For") ?? this.Context.RemoteEndPoint.ToString();

        return this.Player != null
                   ? $"{this.Player.Name}({this.Player?.ID})"
                   : $"<player null - {endpoint}>";
    }

    public void RunTick() {
        var module = Server.Instance.Module;

        if (this.Player is not null) {
            this.DisconnectTicks++;
        }

        if (this.QueuedAnimation is not null) {
            module.BroadcastInStage(this.Context, this.QueuedAnimation);
            this.QueuedAnimation = null;
        }

        if (this.QueuedScoreUpdate is not null) {
            module.BroadcastInStage(this.Context, this.QueuedScoreUpdate);
            this.LastScoreUpdate = this.QueuedScoreUpdate;
            this.QueuedScoreUpdate = null;
        }

        if (this.QueuedVisualUpdate is not null) {
            module.BroadcastInStage(this.Context, this.QueuedVisualUpdate);
            this.QueuedVisualUpdate = null;
        }
    }
}
