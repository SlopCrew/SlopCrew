using SlopCrew.Common;
using SlopCrew.Common.Proto;
using Timer = System.Timers.Timer;

namespace SlopCrew.Server.Encounters;

public class ScoreBattleEncounter : Encounter {
    private NetworkClient one;
    private NetworkClient two;
    private Score oneScore;
    private Score twoScore;
    private uint length;
    private Timer timer;

    public ScoreBattleEncounter(NetworkClient one, NetworkClient two, uint length) {
        this.length = length;
        
        var timerLength = Constants.SimpleEncounterStartTime + this.length;
        this.timer = new Timer(timerLength * 1000);
        this.timer.Elapsed += (_, _) => this.Stop();
        
        this.one = one;
        this.two = two;
        this.Clients.Add(one);
        this.Clients.Add(two);
        
        this.SendStart();
    }

    public override void Update() {
        base.Update();
        if (!this.one.IsConnected() || !this.two.IsConnected()) {
            this.Stop();
        }
    }

    public override void Dispose() {
        if (!this.Finished) this.Stop();
        this.timer.Dispose();
    }

    private void SendStart() {
        this.one.SendPacket(new ClientboundMessage {
            EncounterStart = new ClientboundEncounterStart {
                Type = EncounterType.ScoreBattle,
                Simple = new SimpleEncounterStartData {
                    PlayerId = this.two.Player!.Id
                }
            }
        });

        this.two.SendPacket(new ClientboundMessage {
            EncounterStart = new ClientboundEncounterStart {
                Type = EncounterType.ScoreBattle,
                Simple = new SimpleEncounterStartData {
                    PlayerId = this.one.Player!.Id
                }
            }
        });
    }

    public override void ProcessPacket(NetworkClient client, ServerboundEncounterUpdate packet) {
        if (packet.Type != EncounterType.ScoreBattle) return;
        if (packet.DataCase != ServerboundEncounterUpdate.DataOneofCase.Simple) return;

        if (client == this.one) {
            this.oneScore = packet.Simple.Score;
        } else if (client == this.two) {
            this.twoScore = packet.Simple.Score;
        }

        this.SendUpdate();
    }

    private void SendUpdate() {
        if (this.Finished) return;
        
        if (this.one.IsConnected()) {
            this.one.SendPacket(new ClientboundMessage {
                EncounterUpdate = new ClientboundEncounterUpdate {
                    Type = EncounterType.ScoreBattle,
                    Simple = new ClientboundSimpleEncounterUpdateData {
                        YourScore = this.oneScore,
                        OpponentScore = this.twoScore
                    }
                }
            });
        }

        if (this.two.IsConnected()) {
            this.two.SendPacket(new ClientboundMessage {
                EncounterUpdate = new ClientboundEncounterUpdate {
                    Type = EncounterType.ScoreBattle,
                    Simple = new ClientboundSimpleEncounterUpdateData {
                        YourScore = this.twoScore,
                        OpponentScore = this.oneScore
                    }
                }
            });
        }
    }
    
    private void Stop() {
        this.SendUpdate();
        this.Finished = true;
    }
}
