using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SlopCrew.Common;

public class PlayerNameFilter {
    private static List<string> BannedWords = LoadBannedWords();

    // These people's names get caught in the profanity filter - let's let them through
    private static List<String> BasedNames = new() {
        "gangbangeronline",
        "<#00ff37>DICK <#f4fff2>GRIPPA",
        "undies",
        "[JKS]<#FF0>K1LL<#000>B1LL",
        "[JKS] <color=lightblue>SpookPPL",
        "Start9",
        "Jazzy Senpai",
        "Itz Vexx",
        "<color=#F00>Humongous Slopper"
    };

    // Regex out rich tags that can be abused
    private static List<Regex> Regexes = new() {
        new Regex("<a href*?>"),
        new Regex("<alpha*?>"),
        new Regex("<br>"),
        new Regex("<cspace.*?>"),
        new Regex("<font .*?>"),
        new Regex("<indent.*?>"),
        new Regex("<line-.*?>"),
        new Regex("<margin.*?>"),
        new Regex("<mark.*?>"),
        new Regex("<mspace.*?>"),
        new Regex("<pos.*?>"),
        new Regex("<rotate.*?>"),
        new Regex("<size.*?>"),
        new Regex("<space.*?>"),
        new Regex("<style.*?>"),
        new Regex("<voffset.*?>"),
        new Regex("<width.*?>")
    };

    private static List<string> LoadBannedWords() {
        var result = new List<string>();

        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SlopCrew.Common.res.profanity.txt");
        if (stream is null) throw new Exception("Could not load profanity filter");
        var text = new StreamReader(stream).ReadToEnd();

        var lines = text.Split('\n');
        foreach (var line in lines) {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("#")) continue;
            result.Add(trimmed);
        }

        return result;
    }

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
        return ContainsProfanity(name);
    }

    public static bool ContainsProfanity(string text) {
        foreach (var line in BannedWords) {
            if (text.ToLower().Contains(line.ToLower())) return true;
        }

        return false;
    }
}
