using System.Runtime.CompilerServices;
using SlopCrew.Common;
using SlopCrew.Common.Proto;

namespace SlopCrew.Server;

public class NetworkClient : IDisposable {
    public string? PluginVersion;
    public uint Connection;

    public Player? Player;
    public int? Stage;
    public string? Key;

    private NetworkService networkService;
    
    public NetworkClient(NetworkService networkService) {
        this.networkService = networkService;
    }
    
    public void Dispose() {
        
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
                if (this.Stage is null) break; // how?
                this.networkService.QueuePositionUpdate(this.Stage.Value, packet.PositionUpdate.Update);
                break;
            }
        }
    }

    private void HandleHello(ServerboundHello hello) {
        var player = hello.Player;

        var oldStage = this.Stage;
        this.Stage = hello.Stage;

        player.Id = this.networkService.GetNextFreeID();

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
        System.Numerics.Vector3 pos = tf.Position;
        System.Numerics.Vector3 vel = tf.Velocity;
        for (var i = 0; i < 3; i++) {
            if (!float.IsFinite(pos[i])) pos[i] = 0;
            if (!float.IsFinite(vel[i])) vel[i] = 0;
        }
        player.Transform.Position = new(pos);
        player.Transform.Velocity = new(vel);

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

    public void Disconnect() => this.networkService.Disconnect(this.Connection);

    public void SendPacket(ClientboundMessage packet, SendFlags flags = SendFlags.Reliable) =>
        this.networkService.SendPacket(this.Connection, packet, flags);
}
