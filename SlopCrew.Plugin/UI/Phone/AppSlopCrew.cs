using System;
using Reptile;
using Reptile.Phone;

namespace SlopCrew.Plugin.UI.Phone;

public class AppSlopCrew : App {
    public override void Awake() {
        this.m_Unlockables = Array.Empty<AUnlockable>();
        base.Awake();
    }

    public override void OnPressRight() {
        Plugin.Log.LogInfo("press right");
    }
}
