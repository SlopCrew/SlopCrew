using System.Text.Json.Serialization;
using SlopCrew.Common.Network.Clientbound;
using SlopCrew.Common.Network.Serverbound;

namespace SlopCrew.Common.Network;

[JsonDerivedType(typeof(ClientboundPlayerAnimation), nameof(ClientboundPlayerAnimation))]
[JsonDerivedType(typeof(ClientboundPlayerPositionUpdate), nameof(ClientboundPlayerPositionUpdate))]
[JsonDerivedType(typeof(ClientboundPlayersUpdate), nameof(ClientboundPlayersUpdate))]
[JsonDerivedType(typeof(ServerboundAnimation), nameof(ServerboundAnimation))]
[JsonDerivedType(typeof(ServerboundPlayerHello), nameof(ServerboundPlayerHello))]
[JsonDerivedType(typeof(ServerboundPositionUpdate), nameof(ServerboundPositionUpdate))]
public class NetworkMessage {
    public NetworkMessage() { }
}
