using System;

namespace SlopCrew.API;

public class APIManager {
    public static ISlopCrewAPI? API;
    public static event Action<ISlopCrewAPI>? OnAPIRegistered;

    public static void RegisterAPI(ISlopCrewAPI api) {
        API = api;
        OnAPIRegistered?.Invoke(api);
    }
}
