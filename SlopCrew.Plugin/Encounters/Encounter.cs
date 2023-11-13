using System.Globalization;
using Reptile;
using SlopCrew.Common.Proto;

namespace SlopCrew.Plugin.Encounters;

public abstract class Encounter {
    public EncounterType Type;
    public bool IsBusy;

    public virtual void Update() { }

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

    public virtual void HandleUpdate(ClientboundEncounterUpdate update) { }

    public virtual void HandleEnd(ClientboundEncounterEnd end) {
        this.Stop();
    }

    public virtual void Dispose() {
        Core.OnUpdate -= this.Update;
        StageManager.OnStageInitialized -= this.Stop;
    }
}
