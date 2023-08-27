using System.IO;

namespace SlopCrew.Common.Network.Serverbound;

public class ServerboundPlayerHello : NetworkPacket {
    public override NetworkMessageType MessageType => NetworkMessageType.ServerboundPlayerHello;

    public Player Player;
    public string SecretCode;

    public override void Read(BinaryReader br) {
        var player = new Player();
        player.Read(br);
        this.Player = player;

        this.SecretCode = br.ReadString();
    }

    public override void Write(BinaryWriter bw) {
        this.Player.Write(bw);
        bw.Write(this.SecretCode);
    }
}
