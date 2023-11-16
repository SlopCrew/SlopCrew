using UnityEngine;

namespace SlopCrew.Plugin.UI;

public class UISpinny : MonoBehaviour {
    private void Update() {
        var delta = Time.deltaTime;
        var tf = this.transform;
        tf.RotateAround(tf.position, tf.forward, delta * 360 * 0.25f);
    }
}
