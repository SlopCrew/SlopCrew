using System.Threading;
using System.Threading.Tasks;
using BepInEx.Logging;
using Microsoft.Extensions.Hosting;
using Reptile;
using SlopCrew.Common.Proto;
using SlopCrew.Plugin.UI.Phone;

namespace SlopCrew.Plugin.Encounters;

public class EncounterManager : IHostedService {
    private ManualLogSource logger;
    private Score? lastScore;

    // TODO: SimpleEncounter DI, this shit sucks
    public PlayerManager PlayerManager;
    public ConnectionManager ConnectionManager;
    public ServerConfig ServerConfig;
    public Config Config;
    public InputBlocker InputBlocker;

    public Encounter? CurrentEncounter;

    public EncounterManager(
        ManualLogSource logger,
        PlayerManager playerManager,
        ConnectionManager connectionManager,
        ServerConfig serverConfig,
        Config config,
        InputBlocker inputBlocker
    ) {
        this.logger = logger;
        this.PlayerManager = playerManager;
        this.ConnectionManager = connectionManager;
        this.ServerConfig = serverConfig;
        this.Config = config;
        this.InputBlocker = inputBlocker;
    }

    public Task StartAsync(CancellationToken cancellationToken) {
        Core.OnUpdate += this.Update;
        this.ConnectionManager.Tick += this.Tick;
        this.ConnectionManager.MessageReceived += this.MessageReceived;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) {
        Core.OnUpdate -= this.Update;
        this.ConnectionManager.Tick -= this.Tick;
        this.ConnectionManager.MessageReceived -= this.MessageReceived;
        return Task.CompletedTask;
    }

    private void Update() {
        if (this.CurrentEncounter is { IsBusy: false }) {
            this.CurrentEncounter.Dispose();
            this.CurrentEncounter = null;
        }

        this.CurrentEncounter?.Update();
    }

    private void StartEncounter(ClientboundEncounterStart start) {
        if (this.CurrentEncounter is not null) {
            this.logger.LogWarning("StartEncounter with encounter ongoing???");
            return;
        }

        switch (start.Type) {
            case EncounterType.ScoreBattle: {
                    this.CurrentEncounter = new ScoreBattleEncounter(this, start);
                    break;
                }

            case EncounterType.ComboBattle: {
                    this.CurrentEncounter = new ComboBattleEncounter(this, start);
                    break;
                }

            case EncounterType.Race: {
                    this.CurrentEncounter = new RaceEncounter(this, start);
                    break;
                }
        }
    }

    private void Tick() {
        if (this.CurrentEncounter is SimpleEncounter encounter) {
            var me = WorldHandler.instance.GetCurrentPlayer();
            if (me == null) return;

            var score = (int) me.score;
            var baseScore = (int) me.baseScore;
            var multiplier = (int) me.scoreMultiplier;
            var diff = this.lastScore is null
                       || this.lastScore.Score_ != score
                       || this.lastScore.BaseScore != baseScore
                       || this.lastScore.Multiplier != multiplier;

            if (diff) {
                this.lastScore = new Score {
                    Score_ = score,
                    BaseScore = baseScore,
                    Multiplier = multiplier
                };

                this.ConnectionManager.SendMessage(new ServerboundMessage {
                    EncounterUpdate = new ServerboundEncounterUpdate {
                        Type = encounter.Type,
                        Simple = new ServerboundSimpleEncounterUpdateData {
                            Score = this.lastScore
                        }
                    }
                });
            }
        } else {
            this.lastScore = null;
        }
    }

    private void MessageReceived(ClientboundMessage message) {
        switch (message.MessageCase) {
            case ClientboundMessage.MessageOneofCase.EncounterRequest: {
                    var me = WorldHandler.instance.GetCurrentPlayer();
                    if (me == null) return;
                    var app = me.phone.GetAppInstance<AppEncounters>();
                    if (app == null) return;
                    app.HandleEncounterRequest(message.EncounterRequest);
                    break;
                }

            case ClientboundMessage.MessageOneofCase.EncounterStart: {
                    this.StartEncounter(message.EncounterStart);
                    break;
                }

            case ClientboundMessage.MessageOneofCase.EncounterUpdate: {
                    this.CurrentEncounter?.HandleUpdate(message.EncounterUpdate);
                    break;
                }

            case ClientboundMessage.MessageOneofCase.EncounterEnd: {
                    this.CurrentEncounter?.HandleEnd(message.EncounterEnd);
                    break;
                }
        }
    }
}
