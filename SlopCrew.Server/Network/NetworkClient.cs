using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Connections;
using SlopCrew.Common;
using SlopCrew.Common.Proto;

namespace SlopCrew.Server;

public class NetworkClient : IDisposable {
    public string? PluginVersion;
    public uint Connection;

    public Player? Player;
    public int? Stage;
    public string? Key;
    public ulong Latency;

    private NetworkService networkService;
    private TickRateService tickRateService;

    public NetworkClient(NetworkService networkService, TickRateService tickRateService) {
        this.networkService = networkService;
        this.tickRateService = tickRateService;
    }

    public void Dispose() { }

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
                        // TODO config this
                        TickRate = 10,
                        BannedPlugins = { }
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
                this.Latency = (now - sentAt) * 2;
                this.SendPacket(new ClientboundMessage {
                    Pong = new ClientboundPong {
                        Id = packet.Ping.Id,
                        Tick = this.tickRateService.CurrentTick
                    }
                });
                break;
            }
        }
    }

    private void HandleHello(ServerboundHello hello) {
        var player = hello.Player;

        var oldStage = this.Stage;
        this.Stage = hello.Stage;

        if (this.Player?.Id is null) {
            player.Id = this.networkService.GetNextFreeID();
            Console.WriteLine($"Player {player.Name} assigned ID {player.Id}");
        } else {
            player.Id = this.Player.Id;
        }

        // Cap a few things for people who are naughty
        var customCharacterInfo = player.CustomCharacterInfo.ToList();
        if (customCharacterInfo.Count > Constants.MaxCustomCharacterInfo) {
            customCharacterInfo.RemoveRange(
                Constants.MaxCustomCharacterInfo,
                customCharacterInfo.Count - Constants.MaxCustomCharacterInfo
            );
        }
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
}
