using System;
using System.Collections.Generic;

namespace SlopCrew.Common; 

public static class PlayerNameFilter {
    private static ProfanityFilter.ProfanityFilter Filter = new();
    
    // these names are caught in the filter. exclude them.
    private static List<String> BasedNames = new() {
        "gangbangeronline",
        "<color=#00ff37>DICK <color=#f4fff2>GRIPPA"
    };
    
    public static string DoFilter(string name) {
        if (BasedNames.Contains(name)) {
            return name;
        }
        
        if (Filter.ContainsProfanity(name.ToLower())) {
            return "Punished Slopper";
        }

        var len = Math.Min(32, name.Length);
        return name.Substring(0, len);
    }
}
