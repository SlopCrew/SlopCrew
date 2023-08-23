using System.IO;

namespace SlopCrew.Common.Network.Serverbound;

public class ServerboundPlayerHello : NetworkPacket {
    public override NetworkMessageType MessageType => NetworkMessageType.ServerboundPlayerHello;

    public Player Player;

    public override void Read(BinaryReader br) {
        var player = new Player();
        player.Read(br);
        this.Player = player;
    }

    public override void Write(BinaryWriter bw) {
        this.Player.Write(bw);
    }
}
