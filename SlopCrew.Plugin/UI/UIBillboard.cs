using Reptile;
using UnityEngine;

namespace SlopCrew.Plugin.UI;

public class UIBillboard : MonoBehaviour {
    private void Update() {
        var camera = WorldHandler.instance.CurrentCamera;
        if (camera is null) return;

        var rot = Quaternion.LookRotation(camera.transform.forward, Vector3.up);
        rot = Quaternion.Euler(0, rot.eulerAngles.y, 0);
        this.gameObject.transform.rotation = rot;
    }
}
