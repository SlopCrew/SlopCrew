using System;
using Reptile;
using Reptile.Phone;
using TMPro;

namespace SlopCrew.Plugin.UI.Phone;

public class AppSlopCrew : App {
    public TextMeshProUGUI? Label;
    
    public override void Awake() {
        this.m_Unlockables = Array.Empty<AUnlockable>();
        base.Awake();
    }

    public override void OnPressRight() {
        Plugin.Log.LogInfo("press right");
    }
}
