using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SlopCrew.Common;

// NOTE(NotNite): This class is in common to allow you to see if your name gets filtered on the title screen
// This also adds a *lot* of bloat to the plugin, so let's re-evaluate if it should be here
public static class PlayerNameFilter {
    private static ProfanityFilter.ProfanityFilter Filter = new();

    // These people's names get caught in the profanity filter - let's let them through
    private static List<String> BasedNames = new() {
        "gangbangeronline",
        "<color=#00ff37>DICK <color=#f4fff2>GRIPPA",
        "undies",
        "[JKS]<#FF0>K1LL<#000>B1LL",
        "[JKS] <color=lightblue>SpookPPL"
    };
    
    // Regex out some tags
    private static List<Regex> Regexes = new() {
        new Regex("<size.*?>")
    };

    public static string DoFilter(string name) {
        if (HitsFilter(name)) return Constants.CensoredName;

        var regexed = name;
        foreach (var regex in Regexes) {
            regexed = regex.Replace(regexed, "");
        }

        var len = Math.Min(Constants.NameLimit, regexed.Length);
        return regexed.Substring(0, len);
    }

    public static bool HitsFilter(string name) {
        if (BasedNames.Contains(name)) return false;
        return Filter.ContainsProfanity(name.ToLower());
    }
}
