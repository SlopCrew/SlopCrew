using System.IO;

namespace SlopCrew.Common.Network.Clientbound;

public class ClientboundPlayerScoreUpdate : NetworkPacket {
    public override NetworkMessageType MessageType => NetworkMessageType.ClientboundPlayerScoreUpdate;

    public uint Player;
    public int Score;
    public int BaseScore;
    public int Multiplier;

    public override void Read(BinaryReader br) {
        this.Player = br.ReadUInt32();
        this.Score = br.ReadInt32();
        this.BaseScore = br.ReadInt32();
        this.Multiplier = br.ReadInt32();
    }

    public override void Write(BinaryWriter bw) {
        bw.Write(this.Player);
        bw.Write(this.Score);
        bw.Write(this.BaseScore);
        bw.Write(this.Multiplier);
    }
}
