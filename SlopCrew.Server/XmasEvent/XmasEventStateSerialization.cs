using System.Text.Json;

namespace SlopCrew.Server.XmasEvent;

/// <summary>
/// Methods to serialize event state to and from JSON so it can be persisted to disk server-side.
/// </summary>
public class XmasEventStateSerializer {
    public static string ToJson(XmasServerEventStatePacket state) {
        return JsonSerializer.Serialize(state);
    }
    
    public static XmasServerEventStatePacket FromJson(string json) {
        return JsonSerializer.Deserialize<XmasServerEventStatePacket>(json)!;
    }
}
