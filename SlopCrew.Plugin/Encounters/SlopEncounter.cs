using System;
using Reptile;
using SlopCrew.Common.Encounters;

namespace SlopCrew.Plugin.Encounters;

public class SlopEncounter : IDisposable {
    public bool IsBusy { get; protected set; }

    protected SlopEncounter(EncounterConfigData configData) {
        Core.OnUpdate += this.Update;
        StageManager.OnStagePostInitialization += this.Stop;
    }

    protected virtual void Update() { }

    protected virtual void Stop() {
        this.IsBusy = false;
    }

    public virtual void Dispose() { }
}
