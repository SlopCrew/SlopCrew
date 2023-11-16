using System.Timers;
using SlopCrew.Common;
using SlopCrew.Common.Proto;
using Timer = System.Timers.Timer;

namespace SlopCrew.Server.Encounters; 

public class Lobby : IDisposable {
    private List<NetworkClient> clients = new();

    private EncounterService encounterService;
    private int stage;
    private EncounterType encounterType;

    private int timeLeft = Constants.LobbyMaxWaitTime;
    private Timer timer;

    public Lobby(EncounterService encounterService, int stage, EncounterType encounterType) {
        this.encounterService = encounterService;
        this.stage = stage;
        this.encounterType = encounterType;

        this.timer = new Timer();
        this.timer.Interval = 1000;
        this.timer.Elapsed += this.TimerElapsed;
    }

    public void Dispose() {
        this.timer.Elapsed -= this.TimerElapsed;
        this.timer.Dispose();
    }

    public void Update() {
        this.clients = this.clients.Where(x => x.Stage == this.stage && x.IsConnected()).ToList();
        if (this.clients.Count < 1) {
            this.timer.Stop();
            this.timeLeft = Constants.LobbyMaxWaitTime;
        }
    }

    private void TransferToEncounter() {
        switch (this.encounterType) {
            case EncounterType.Race: {
                var config = this.encounterService.RaceConfigService.GetRaceConfig(this.stage);
                var encounter = new RaceEncounter(new List<NetworkClient>(this.clients), this.stage, config);
                this.encounterService.TrackEncounter(encounter);
                foreach (var client in this.clients) client.CurrentEncounter = encounter;
                break;
            }
        }

        this.clients.Clear();
        this.timer.Stop();
    }
    
    public void StartTimer() {
        this.timeLeft = Constants.LobbyMaxWaitTime;
        this.timer.Start();
    }

    private void TimerElapsed(object? sender, ElapsedEventArgs @event) {
        this.timeLeft -= 1;
        if (this.timeLeft <= 0) {
            this.timer.Stop();
            this.TransferToEncounter();
        }
    }

    public void AddPlayer(NetworkClient client) {
        if (!this.timer.Enabled) this.StartTimer();
        if (this.clients.Contains(client)) return;
        this.clients.Add(client);
        this.timeLeft = Math.Min(Constants.LobbyMaxWaitTime, this.timeLeft + Constants.LobbyIncrementWaitTime);
    }
}
