using System.Collections.Generic;
using System.IO;

namespace SlopCrew.Common.Network.Clientbound;

public class ClientboundPlayersUpdate : NetworkPacket {
    public override NetworkMessageType MessageType => NetworkMessageType.ClientboundPlayersUpdate;
    
    public List<Player> Players;

    public override void Read(BinaryReader br) {
        var len = br.ReadInt32();
        this.Players = new List<Player>(len);
        for (var i = 0; i < len; i++) {
            var player = new Player();
            player.Read(br);
            this.Players.Add(player);
        }
    }

    public override void Write(BinaryWriter bw) {
        bw.Write(this.Players.Count);
        foreach (var player in this.Players) {
            player.Write(bw);
        }
    }
}
