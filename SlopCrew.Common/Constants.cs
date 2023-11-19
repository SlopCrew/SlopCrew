using System.Collections.Generic;
using SlopCrew.Common.Proto;

namespace SlopCrew.Common;

public class Constants {
    public const uint NetworkVersion = 4;
    public const int MaxCustomCharacterInfo = 5;
    public const int MaxCustomPacketSize = 512;

    public const string DefaultName = "Big Slopper";
    public const string CensoredName = "Punished Slopper";
    public const int NameLimit = 32;

    public const int PingFrequency = 5000;
    public const int ReconnectFrequency = 5000;

    public const int SimpleEncounterStartTime = 3;
    public const int SimpleEncounterEndTime = 5;
    public const int LobbyMaxWaitTime = 30;
    public const int LobbyIncrementWaitTime = 5;
    public const int RaceEncounterStartTime = 3;
    public const int MaxRaceTime = 120;

    public static Dictionary<QuickChatCategory, List<string>> QuickChatMessages = new() {
        {QuickChatCategory.General, ["Heya!", "Goodbye!", "Good game!", "OK.", "No thanks."]},
        // line break for formatting lol
        {
            QuickChatCategory.Actions,
            ["Let's chill!", "Let's skate!", "Let's score battle!", "Let's combo battle!", "Let's race!"]
        },
        // line break for formatting lol
        {
            QuickChatCategory.Places,
            [
                "Police Station", "Hideout", "Versum Hill", "Millenium Square", "Brink Terminal", "Millenium Mall",
                "Mataan", "Pyramid Island", "Jakes"
            ]
        },
        // line break for formatting lol
        {
            QuickChatCategory.Emojis,
            [
                "<sprite=0>",
                "<sprite=1>",
                "<sprite=2>",
                "<sprite=3>",
                "<sprite=4>",
                "<sprite=5>",
                "<sprite=6>",
                "<sprite=7>",
                "<sprite=8>",
                "<sprite=9>",
                "<sprite=10>",
                "<sprite=11>",
                "<sprite=12>",
                "<sprite=13>",
                "<sprite=14>",
                "<sprite=15>"
            ]
        }
    };
}
