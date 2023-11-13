using SlopCrew.Common;
using SlopCrew.Common.Proto;
using Timer = System.Timers.Timer;

namespace SlopCrew.Server.Encounters;

public class ComboBattleEncounter : Encounter {
    private NetworkClient one;
    private NetworkClient two;
    private Score oneScore = new();
    private Score twoScore = new();
    private bool oneComboDropped;
    private bool twoComboDropped;
    private bool grace;
    private uint length;
    private Timer timer;

    public ComboBattleEncounter(NetworkClient one, NetworkClient two, uint length, uint grace) {
        this.length = length;
        this.grace = true;

        // lmao this is so bad
        Task.Run(async () => {
            await Task.Delay((int) (grace * 1000));
            this.grace = false;
        });
        
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

        if (this.oneComboDropped && this.twoComboDropped) {
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
                Type = EncounterType.ComboBattle,
                Simple = new SimpleEncounterStartData {
                    PlayerId = this.two.Player!.Id
                }
            }
        });

        this.two.SendPacket(new ClientboundMessage {
            EncounterStart = new ClientboundEncounterStart {
                Type = EncounterType.ComboBattle,
                Simple = new SimpleEncounterStartData {
                    PlayerId = this.one.Player!.Id
                }
            }
        });
    }

    public override void ProcessPacket(NetworkClient client, ServerboundEncounterUpdate packet) {
        if (packet.Type != EncounterType.ComboBattle) return;
        if (packet.DataCase != ServerboundEncounterUpdate.DataOneofCase.Simple) return;

        if (client == this.one) {
            if (!this.oneComboDropped) {
                if (this.CalculateComboDrop(this.oneScore, packet.Simple.Score)) {
                    this.oneComboDropped = true;
                } else {
                    this.oneScore = packet.Simple.Score;
                }
            }
        } else if (client == this.two) {
            if (!this.twoComboDropped) {
                if (this.CalculateComboDrop(this.twoScore, packet.Simple.Score)) {
                    this.twoComboDropped = true;
                } else {
                    this.twoScore = packet.Simple.Score;
                }
            }
        }

        this.SendUpdate();
    }

    private bool CalculateComboDrop(Score oldScore, Score newScore) {
        if (this.grace) return false;
        return newScore.BaseScore * newScore.Multiplier < oldScore.BaseScore * oldScore.Multiplier;
    }

    private void SendUpdate() {
        if (this.Finished) return;

        if (this.one.IsConnected()) {
            this.one.SendPacket(new ClientboundMessage {
                EncounterUpdate = new ClientboundEncounterUpdate {
                    Type = EncounterType.ComboBattle,
                    Simple = new ClientboundSimpleEncounterUpdateData {
                        YourScore = this.oneScore,
                        OpponentScore = this.twoScore,
                        YourComboDropped = this.oneComboDropped,
                        OpponentComboDropped = this.twoComboDropped
                    }
                }
            });
        }

        if (this.two.IsConnected()) {
            this.two.SendPacket(new ClientboundMessage {
                EncounterUpdate = new ClientboundEncounterUpdate {
                    Type = EncounterType.ComboBattle,
                    Simple = new ClientboundSimpleEncounterUpdateData {
                        YourScore = this.twoScore,
                        OpponentScore = this.oneScore,
                        YourComboDropped = this.twoComboDropped,
                        OpponentComboDropped = this.oneComboDropped
                    }
                }
            });
        }
    }

    private void SendEnd() {
        if (this.one.IsConnected()) {
            this.one.SendPacket(new ClientboundMessage {
                EncounterEnd = new ClientboundEncounterEnd {
                    Type = EncounterType.ComboBattle,
                    Simple = new SimpleEncounterEndData {
                        EndedEarly = false, // TODO
                        YourScore = this.oneScore,
                        OpponentScore = this.twoScore
                    }
                }
            });
        }

        if (this.two.IsConnected()) {
            this.two.SendPacket(new ClientboundMessage {
                EncounterEnd = new ClientboundEncounterEnd {
                    Type = EncounterType.ComboBattle,
                    Simple = new SimpleEncounterEndData {
                        EndedEarly = false, // TODO
                        YourScore = this.twoScore,
                        OpponentScore = this.oneScore
                    }
                }
            });
        }
    }

    private void Stop() {
        this.SendUpdate();
        this.SendEnd();
        this.Finished = true;
    }
}
