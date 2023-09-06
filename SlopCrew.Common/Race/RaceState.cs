namespace SlopCrew.Common.Race {
    public enum RaceState {
        None,
        WaitingForRace,
        WaitingForPlayers,
        LoadingStage,
        WaitingForPlayersToBeReady,
        Starting,
        Racing,
        WaitingForFullRanking,
        ShowRanking,
        Finished,
        ForcedFinish,
        Aborted
    }
}
