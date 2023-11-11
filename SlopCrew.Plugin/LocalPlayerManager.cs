using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Reptile;
using SlopCrew.API;
using SlopCrew.Common;
using SlopCrew.Common.Proto;
using Player = SlopCrew.Common.Proto.Player;

namespace SlopCrew.Plugin;

public class LocalPlayerManager : IHostedService {
    public bool HelloRefreshQueued;
    public int CurrentOutfit;
    private SlopConnectionManager connectionManager;

    private Transform? lastTransform;
    private int? lastAnimation;

    public LocalPlayerManager(SlopConnectionManager connectionManager) {
        this.connectionManager = connectionManager;
    }

    public Task StartAsync(CancellationToken cancellationToken) {
        this.connectionManager.Tick += this.Tick;
        this.connectionManager.MessageReceived += this.MessageReceived;
        StageManager.OnStagePostInitialization += this.StagePostInit;
        return Task.CompletedTask;
    }


    public Task StopAsync(CancellationToken cancellationToken) {
        this.connectionManager.Tick -= this.Tick;
        this.connectionManager.MessageReceived -= this.MessageReceived;
        StageManager.OnStagePostInitialization -= this.StagePostInit;
        return Task.CompletedTask;
    }

    private void MessageReceived(ClientboundMessage message) {
        if (message.MessageCase is ClientboundMessage.MessageOneofCase.Hello) {
            this.HelloRefreshQueued = true;
        }
    }

    private void Tick() {
        var worldHandler = WorldHandler.instance;
        if (worldHandler == null) return;
        var me = worldHandler.GetCurrentPlayer();
        if (me == null) return;

        if (this.HelloRefreshQueued) {
            this.connectionManager.SendMessage(new ServerboundMessage {
                Hello = new ServerboundHello {
                    Player = new Player {
                        // FIXME im too lazy to write a config file lmao
                        Name = Directory.GetCurrentDirectory().Contains("gog")
                                   ? "GOG Slopper"
                                   : "Big Slopper",

                        Transform = new Transform {
                            Position = new(me.tf.position.FromMentalDeficiency()),
                            Rotation = new(me.tf.rotation.FromMentalDeficiency()),
                            Velocity = new(me.motor.velocity.FromMentalDeficiency())
                        },

                        CharacterInfo = new CharacterInfo {
                            Character = (int) me.character,
                            Outfit = this.CurrentOutfit,
                            MoveStyle = (int) me.moveStyle
                        }
                    },

                    Stage = APIManager.API!.StageOverride ?? (int) Core.instance.baseModule.CurrentStage
                }
            });

            this.HelloRefreshQueued = false;
        }

        var transform = me.tf;
        var newPos = transform.position.FromMentalDeficiency();
        var newRot = transform.rotation.FromMentalDeficiency();
        var newVel = me.motor.velocity.FromMentalDeficiency();

        const float minDistance = 0.01f;

        if (this.lastTransform == null
            || (this.lastTransform.Position - newPos).LengthSquared() > minDistance
            || (this.lastTransform.Rotation - newRot).LengthSquared() > minDistance
           ) {
            var newTransform = new Transform {
                Position = new(newPos),
                Rotation = new(newRot),
                Velocity = new(newVel)
            };

            this.connectionManager.SendMessage(new ServerboundMessage {
                PositionUpdate = new ServerboundPositionUpdate {
                    Update = new PositionUpdate {
                        Transform = newTransform,
                        Tick = this.connectionManager.ServerTick,
                        Latency = this.connectionManager.Latency
                    }
                }
            }, flags: SendFlags.Unreliable);

            this.lastTransform = newTransform;
        }
    }

    private void StagePostInit() {
        this.HelloRefreshQueued = true;
    }

    public void PlayAnim(int anim, bool forceOverwrite, bool instant, float atTime) {
        // Sometimes the game spams animation
        if (this.lastAnimation == anim) return;
        this.lastAnimation = anim;

        this.connectionManager.SendMessage(new ServerboundMessage {
            AnimationUpdate = new ServerboundAnimationUpdate {
                Update = new AnimationUpdate {
                    Animation = anim,
                    ForceOverwrite = forceOverwrite,
                    Instant = instant,
                    Time = atTime
                }
            }
        });
    }
}
