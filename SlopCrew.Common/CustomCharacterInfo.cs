using System.IO;
using SlopCrew.Common.Network;

namespace SlopCrew.Common; 

public class CustomCharacterInfo : NetworkSerializable {
    public CustomCharacterMethod Method;
    public string Data;

    public override void Read(BinaryReader br) {
        this.Method = (CustomCharacterMethod) br.ReadInt32();
        this.Data = br.ReadString();
    }
    
    public override void Write(BinaryWriter bw) {
        bw.Write((int) this.Method);
        bw.Write(this.Data);
    }

    public enum CustomCharacterMethod {
        None,
        CrewBoom, // https://github.com/SGiygas/CrewBoom
        CharacterAPI //https://github.com/viliger2/BRC_CharacterAPI
    }
}
