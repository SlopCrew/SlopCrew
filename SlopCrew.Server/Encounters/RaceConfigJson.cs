using SlopCrew.Common.Proto;
using Vector3 = System.Numerics.Vector3;

namespace SlopCrew.Server.Encounters;

public record RaceConfigJson {
    public required int Stage;
    public required List<Vector3> MapPins;
    public required Vector3 StartPosition;

    public RaceConfig ToNetworkRaceConfig() {
        return new RaceConfig {
            StartPosition = new(this.StartPosition),
            MapPins = {this.MapPins.Select(x => new Common.Proto.Vector3(x))}
        };
    }
}
