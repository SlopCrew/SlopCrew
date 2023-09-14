using System.Collections.Generic;

namespace SlopCrew.Common;

public class Constants {
    public const int TicksPerSecond = 10;
    public const float TickRate = 1f / TicksPerSecond;
    public const int NameLimit = 32;
    public const string CensoredName = "Punished Slopper";
    public const uint NetworkVersion = 3;

    public static List<string> SecretCodes = new() {
        "8d2bb802bdb88399fc22e0445a83b410d8f23f9befc9a361aa68b48487acedb6"
    };
}
