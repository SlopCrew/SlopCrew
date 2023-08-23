using System.Collections.Generic;

namespace SlopCrew.Common.Network.Clientbound;

public class ClientboundPlayersUpdate : NetworkMessage {
    public ClientboundPlayersUpdate() { }

    public List<Player> Players;
}
