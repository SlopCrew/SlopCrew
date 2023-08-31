using System.IO;

namespace SlopCrew.Common.Network.Serverbound;

public class ServerboundScoreUpdate : NetworkPacket {
    public override NetworkMessageType MessageType => NetworkMessageType.ServerboundScoreUpdate;

    public int Score;
    public int BaseScore;
    public int Multiplier;

    public override void Read(BinaryReader br) {
        this.Score = br.ReadInt32();
        this.BaseScore = br.ReadInt32();
        this.Multiplier = br.ReadInt32();
    }

    public override void Write(BinaryWriter bw) {
        bw.Write(this.Score);
        bw.Write(this.BaseScore);
        bw.Write(this.Multiplier);
    }
}
