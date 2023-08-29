using System;
using System.Collections.Generic;

namespace SlopCrew.Common; 

// NOTE(NotNite): This class is in common to allow you to see if your name gets filtered on the title screen
// This also adds a *lot* of bloat to the plugin, so let's re-evaluate if it should be here
public static class PlayerNameFilter {
    private static ProfanityFilter.ProfanityFilter Filter = new();
    
    // These people's names get caught in the profanity filter - let's let them through
    private static List<String> BasedNames = new() {
        "gangbangeronline",
        "<color=#00ff37>DICK <color=#f4fff2>GRIPPA"
    };
    
    public static string DoFilter(string name) {
        if (BasedNames.Contains(name)) {
            return name;
        }
        
        if (Filter.ContainsProfanity(name.ToLower())) {
            return Constants.CensoredName;
        }

        var len = Math.Min(Constants.NameLimit, name.Length);
        return name.Substring(0, len);
    }
}
