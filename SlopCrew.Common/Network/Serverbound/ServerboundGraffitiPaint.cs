using System.Collections.Generic;
using System.IO;

namespace SlopCrew.Common.Network.Serverbound; 

public class ServerboundGraffitiPaint : NetworkPacket {
    public override NetworkMessageType MessageType => NetworkMessageType.ServerboundGraffitiPaint;
    
    public string GraffitiSpot;
    public List<int> targetsHitSequence;

    public override void Read(BinaryReader br) {
        this.GraffitiSpot = br.ReadString();
        this.targetsHitSequence = new List<int>();
        int count = br.ReadInt32();
        for (int i = 0; i < count; i++) {
            this.targetsHitSequence.Add(br.ReadInt32());
        }
    }

    public override void Write(BinaryWriter bw) {
        bw.Write(this.GraffitiSpot);
        bw.Write(this.targetsHitSequence.Count);
        foreach (var target in this.targetsHitSequence) {
            bw.Write(target);
        }
    }
}
