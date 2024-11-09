using Microsoft.Extensions.Options;
using SlopCrew.Common;
using SlopCrew.Common.Proto;
using SlopCrew.Server.Database;
using SlopCrew.Server.Encounters;
using SlopCrew.Server.Options;

namespace SlopCrew.Server;

public class NetworkClient : IDisposable {
    public string? PluginVersion;
    public uint Connection;
    public string Ip = string.Empty;

    public Player? Player;
    public int? Stage;
    public string? Key;
    public ulong Latency;
    public bool IsCommunityContributor;
    public string? RepresentingCrew;
    public int QuickChatCooldown = 0;

    public Dictionary<EncounterType, List<NetworkClient>> EncounterRequests = new();
    public Encounter? CurrentEncounter;

    private List<string> recentCustomPackets = new();

    private NetworkService networkService;
    private TickRateService tickRateService;
    private EncounterService encounterService;
    private ServerOptions serverOptions;
    private EncounterOptions encounterOptions;
    private IServiceScopeFactory scopeFactory;

    public NetworkClient(
        NetworkService networkService,
        TickRateService tickRateService,
        EncounterService encounterService,
        IOptions<ServerOptions> serverOptions,
        IOptions<EncounterOptions> encounterOptions,
        IServiceScopeFactory scopeFactory
    ) {
        this.networkService = networkService;
        this.tickRateService = tickRateService;
        this.encounterService = encounterService;
        this.serverOptions = serverOptions.Value;
        this.encounterOptions = encounterOptions.Value;
        this.scopeFactory = scopeFactory;

        this.tickRateService.Tick += this.Tick;
    }

    public bool IsConnected() => this.networkService.Clients.Contains(this);

    public void Dispose() {
        this.tickRateService.Tick -= this.Tick;
    }

    public void HandlePacket(ServerboundMessage packet) {
        switch (packet.MessageCase) {
            case ServerboundMessage.MessageOneofCase.Version: {
                if (packet.Version.ProtocolVersion != Constants.NetworkVersion) {
                    this.Disconnect();
                    return;
                }

                this.PluginVersion = packet.Version.PluginVersion;
                this.networkService.SubmitPluginVersionMetrics();

                this.SendPacket(new ClientboundMessage {
                    Hello = new ClientboundHello {
                        TickRate = this.serverOptions.TickRate,
                        BannedPlugins = {this.encounterOptions.BannedPlugins},
                        ScoreBattleLength = this.encounterOptions.ScoreBattleLength,
                        ComboBattleLength = this.encounterOptions.ComboBattleLength,
                        PlayerId = this.Player!.Id
                    }
                });

                break;
            }

            case ServerboundMessage.MessageOneofCase.Hello: {
                this.HandleHello(packet.Hello);
                break;
            }

            case ServerboundMessage.MessageOneofCase.PositionUpdate: {
                if (this.Stage is null || this.Player is null) break; // how?
                var update = packet.PositionUpdate.Update;
                update.PlayerId = this.Player!.Id;
                update.Tick = this.tickRateService.CurrentTick;
                update.Latency = this.Latency;

                var tf = update.Transform;
                this.SafetyCheck(ref tf);
                update.Transform = tf;
                this.Player.Transform = tf;

                this.networkService.QueuePositionUpdate(this.Stage.Value, update);
                break;
            }

            case ServerboundMessage.MessageOneofCase.Ping: {
                var sentAt = packet.Ping.Time;
                var now = (ulong) (DateTime.UtcNow.ToFileTimeUtc() / 10_000);
                //this.Latency = (now - sentAt) * 2;
                this.Latency = 0;
                this.SendPacket(new ClientboundMessage {
                    Pong = new ClientboundPong {
                        Id = packet.Ping.Id,
                        Tick = this.tickRateService.CurrentTick
                    }
                });

                break;
            }

            case ServerboundMessage.MessageOneofCase.VisualUpdate: {
                if (this.Stage is null || this.Player is null) break;
                var update = packet.VisualUpdate.Update;
                update.PlayerId = this.Player!.Id;
                this.networkService.QueueVisualUpdate(this.Stage.Value, update);
                break;
            }

            case ServerboundMessage.MessageOneofCase.AnimationUpdate: {
                if (this.Stage is null || this.Player is null) break;
                var update = packet.AnimationUpdate.Update;
                update.PlayerId = this.Player!.Id;
                this.networkService.QueueAnimationUpdate(this.Stage.Value, update);
                break;
            }

            case ServerboundMessage.MessageOneofCase.EncounterRequest: {
                if (this.CurrentEncounter is not null || this.Player is null || this.Stage is null) return;
                var request = packet.EncounterRequest;

                switch (request.Type) {
                    case EncounterType.ScoreBattle or EncounterType.ComboBattle: {
                        if (request.HasPlayerId) this.ProcessSimpleEncounterRequest(request.Type, request.PlayerId);
                        break;
                    }

                    case EncounterType.Race: {
                        this.encounterService.QueueIntoLobby(this, EncounterType.Race);
                        break;
                    }
                }

                break;
            }

            case ServerboundMessage.MessageOneofCase.EncounterUpdate: {
                this.CurrentEncounter?.ProcessPacket(this, packet.EncounterUpdate);
                break;
            }

            case ServerboundMessage.MessageOneofCase.CustomPacket: {
                if (this.Player is null || this.Stage is null) return;
                var customPacket = packet.CustomPacket.Packet;
                if (customPacket.Data.Length > Constants.MaxCustomPacketSize
                    || customPacket.Id.Length > Constants.MaxCustomPacketSize) return;

                // You can blame Duchess for this
                if (this.recentCustomPackets.Contains(customPacket.Id)) return;
                this.recentCustomPackets.Add(customPacket.Id);

                this.networkService.SendToStage(this.Stage.Value, new ClientboundMessage {
                    CustomPacket = new ClientboundCustomPacket {
                        PlayerId = this.Player.Id,
                        Packet = customPacket
                    }
                }, exclude: this.Player.Id);
                break;
            }

            case ServerboundMessage.MessageOneofCase.QuickChat: {
                if (this.Player is null || this.Stage is null) return;
                if (this.QuickChatCooldown > 0) return;

                var quickChat = packet.QuickChat.QuickChat;
                if (quickChat.Index >= Constants.QuickChatMessages[quickChat.Category].Count) return;
                this.networkService.SendToStage(this.Stage.Value, new ClientboundMessage {
                    QuickChat = new ClientboundQuickChat {
                        PlayerId = this.Player.Id,
                        QuickChat = quickChat
                    }
                });

                this.QuickChatCooldown = this.serverOptions.TickRate * 2;

                break;
            }
        }
    }

    public void ProcessSimpleEncounterRequest(EncounterType type, uint other) {
        var otherPlayer = this.networkService.Clients.FirstOrDefault(x => x.Player?.Id == other);
        if (otherPlayer is null) return;

        if (this.EncounterRequests.TryGetValue(type, out var requests)) {
            if (requests.Contains(otherPlayer)) return;
            requests.Add(otherPlayer);
        } else {
            this.EncounterRequests[type] = new() {otherPlayer};
        }

        Task.Run(async () => {
            await Task.Delay(5000);
            if (this.EncounterRequests.TryGetValue(type, out var requests)) {
                requests.Remove(otherPlayer);
            }
        });

        if (
            otherPlayer.EncounterRequests.TryGetValue(type, out var otherRequests)
            && otherRequests.Contains(this)
        ) {
            otherPlayer.EncounterRequests.Clear();
            this.EncounterRequests.Clear();

            if (this.Player is null || otherPlayer.Player is null) return;

            var encounter = this.encounterService.StartSimpleEncounter(this, otherPlayer, type);
            this.CurrentEncounter = encounter;
            otherPlayer.CurrentEncounter = encounter;
        } else {
            otherPlayer.SendPacket(new ClientboundMessage {
                EncounterRequest = new ClientboundEncounterRequest {
                    Type = type,
                    PlayerId = this.Player!.Id
                }
            });
        }
    }

    private void HandleHello(ServerboundHello hello) {
        var player = hello.Player;

        var oldStage = this.Stage;
        this.Stage = hello.Stage;

        player.Id = this.Player?.Id ?? this.networkService.GetNextFreeID();

        if (hello.HasKey && this.Key is null) {
            this.Key = hello.Key;

            // people's info kept getting voided because I forgot that you can't keep DbContexts around in efcore lol
            using var scope = this.scopeFactory.CreateScope();
            var userService = scope.ServiceProvider.GetRequiredService<UserService>();

            var user = userService.GetUserByKey(this.Key);
            this.IsCommunityContributor = user?.IsCommunityContributor ?? false;
            this.RepresentingCrew = user?.RepresentingCrew?.Tag;
        }

        player.IsCommunityContributor = this.IsCommunityContributor;
        player.RepresentingCrew = this.RepresentingCrew ?? string.Empty;

        var customCharacterInfo = player.CustomCharacterInfo.ToList();
        if (customCharacterInfo.Count > Constants.MaxCustomCharacterInfo) {
            customCharacterInfo.RemoveRange(
                Constants.MaxCustomCharacterInfo,
                customCharacterInfo.Count - Constants.MaxCustomCharacterInfo
            );
        }
        customCharacterInfo = customCharacterInfo
            .Where(x => x.Id.Length <= Constants.MaxCustomPacketSize && x.Data.Length <= Constants.MaxCustomPacketSize)
            .ToList();

        player.CustomCharacterInfo.Clear();
        player.CustomCharacterInfo.AddRange(customCharacterInfo);

        var tf = player.Transform;
        this.SafetyCheck(ref tf);
        player.Transform = tf;

        player.Name = PlayerNameFilter.DoFilter(player.Name);

        if (player.CharacterInfo.Character is < 0 or > 25)
            player.CharacterInfo.Character = 3; // metalHead

        if (player.CharacterInfo.Outfit is < 0 or > 3)
            player.CharacterInfo.Outfit = 0;

        if (player.CharacterInfo.MoveStyle is < 0 or > 4)
            player.CharacterInfo.MoveStyle = 0;

        if (player.CharacterInfo.MoveStyle is 4) // SPECIAL_SKATEBOARD
            player.CharacterInfo.MoveStyle = 2;

        this.Key = hello.HasKey ? hello.Key : null;
        this.Player = player;

        // TODO ratelimit this to once per tick or something
        if (oldStage is not null) this.networkService.BroadcastPlayersInStage(oldStage.Value);
        this.networkService.BroadcastPlayersInStage(this.Stage.Value);
    }

    private void SafetyCheck(ref Transform transform) {
        if (!float.IsFinite(transform.Position.X)) transform.Position.X = 0;
        if (!float.IsFinite(transform.Position.Y)) transform.Position.Y = 0;
        if (!float.IsFinite(transform.Position.Z)) transform.Position.Z = 0;

        if (!float.IsFinite(transform.Rotation.X)) transform.Rotation.X = 0;
        if (!float.IsFinite(transform.Rotation.Y)) transform.Rotation.Y = 0;
        if (!float.IsFinite(transform.Rotation.Z)) transform.Rotation.Z = 0;
        if (!float.IsFinite(transform.Rotation.W)) transform.Rotation.W = 0;
    }

    public void Disconnect() => this.networkService.Disconnect(this.Connection);

    public void SendPacket(ClientboundMessage packet, SendFlags flags = SendFlags.Reliable) =>
        this.networkService.SendPacket(this.Connection, packet, flags);

    private void Tick() {
        if (this.QuickChatCooldown > 0) this.QuickChatCooldown--;
        if (this.CurrentEncounter is {Finished: true}) this.CurrentEncounter = null;
        this.recentCustomPackets.Clear();
    }
}
