using System;
using System.Globalization;
using Reptile;
using SlopCrew.Common.Encounters;
using SlopCrew.Common.Network.Clientbound;

namespace SlopCrew.Plugin.Encounters;

public class SlopEncounter : IDisposable {
    public Guid Guid { get; protected set; }
    public bool IsBusy { get; protected set; }

    protected SlopEncounter(EncounterConfigData configData) {
        this.Guid = configData.Guid;
        Core.OnUpdate += this.Update;
        StageManager.OnStagePostInitialization += this.Stop;
    }

    protected virtual void Update() { }

    protected virtual void Stop() {
        this.IsBusy = false;
    }

    protected string NiceTimerString(double timer) {
        var str = timer.ToString(CultureInfo.CurrentCulture);
        var startIndex = ((int) timer).ToString().Length + 3;
        if (str.Length > startIndex) str = str.Remove(startIndex);
        if (timer == 0.0) str = "0.00";
        return str;
    }

    public virtual void HandleEnd(ClientboundEncounterEnd encounterEnd) {
        this.Stop();
    }

    public virtual void Dispose() { }
}
