using System.Collections.Generic;

namespace SlopCrew.Common;

public class Constants {
    public const float TickRate = 1f / 10f;
    public const int NameLimit = 32;
    public const string CensoredName = "Punished Slopper";
    public const uint NetworkVersion = 2;

    public static List<string> SecretCodes = new() {
        "8d2bb802bdb88399fc22e0445a83b410d8f23f9befc9a361aa68b48487acedb6"
    };
}
