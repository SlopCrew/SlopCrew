using System.Diagnostics;
using System.Timers;
using SlopCrew.Common;
using SlopCrew.Common.Proto;
using Timer = System.Timers.Timer;

namespace SlopCrew.Server.Encounters;

public class RaceEncounter : Encounter {
    private Timer timer;
    private Stopwatch stopwatch;

    private Dictionary<NetworkClient, float> times = new();
    private RaceConfigJson raceConfig;

    public RaceEncounter(List<NetworkClient> clients, int stage, RaceConfigJson raceConfig) {
        this.Clients = clients;
        this.Type = EncounterType.Race;
        this.Stage = stage;
        this.raceConfig = raceConfig;

        this.timer = new Timer();
        this.stopwatch = new Stopwatch();
        this.timer.Interval = Constants.RaceEncounterStartTime;
        this.timer.Elapsed += this.StartStopwatch;

        foreach (var client in this.Clients) {
            client.SendPacket(new ClientboundMessage {
                EncounterStart = new ClientboundEncounterStart {
                    Type = EncounterType.Race,
                    Race = new RaceEncounterStartData {
                        Config = this.raceConfig.ToNetworkRaceConfig()
                    }
                }
            });
        }
    }

    public override void Update() {
        base.Update();
        if (this.stopwatch.Elapsed.TotalSeconds > Constants.MaxRaceTime) this.Stop();
    }

    private void StartStopwatch(object? e, ElapsedEventArgs args) {
        this.timer.Elapsed -= this.StartStopwatch;
        this.stopwatch.Start();
    }

    private IEnumerable<RaceTime> BuildTimes() => this.times
        .Where(x => x.Key.Player is not null && x.Key.Stage == this.Stage && x.Key.IsConnected())
        .Select(x => new RaceTime {
            PlayerId = x.Key.Player!.Id,
            Time = x.Value
        });

    public override void ProcessPacket(NetworkClient client, ServerboundEncounterUpdate packet) {
        if (packet.Type is EncounterType.Race && packet.DataCase is ServerboundEncounterUpdate.DataOneofCase.Race) {
            if (packet.Race.MapPin > this.raceConfig.MapPins.Count) {
                this.times[client] = (float) this.stopwatch.Elapsed.TotalSeconds;

                foreach (var finished in this.times.Keys) {
                    finished.SendPacket(new ClientboundMessage {
                        EncounterUpdate = new ClientboundEncounterUpdate {
                            Type = EncounterType.Race,
                            Race = new ClientboundRaceEncounterUpdateData {
                                Times = {this.BuildTimes()}
                            }
                        }
                    });
                }
            }
        }
    }

    public override void Dispose() {
        if (!this.Finished) this.Stop();
        this.stopwatch.Stop();
        this.timer.Dispose();
    }

    private void Stop() {
        foreach (var client in this.Clients) {
            client.SendPacket(new ClientboundMessage {
                EncounterEnd = new ClientboundEncounterEnd {
                    Type = EncounterType.Race,
                    Race = new ClientboundRaceEncounterUpdateData {
                        Times = {this.BuildTimes()}
                    }
                }
            });
        }

        this.Finished = true;
    }
}
