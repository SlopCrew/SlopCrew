using Reptile;
using SlopCrew.Common;
using TMPro;
using UnityEngine;

namespace SlopCrew.Plugin.UI;

public class UINameplate : MonoBehaviour {
    private TextMeshPro tmp;
    private bool checkedFilter;

    private void Awake() {
        this.tmp = this.transform.Find("SlopCrew_Nameplate").GetComponent<TextMeshPro>();
    }

    private void Update() {
        if (Plugin.SlopConfig.BillboardNameplates.Value) {
            var camera = WorldHandler.instance.CurrentCamera;
            if (camera is null) return;

            var rot = Quaternion.LookRotation(camera.transform.forward, Vector3.up);
            rot = Quaternion.Euler(0, rot.eulerAngles.y, 0);
            this.gameObject.transform.rotation = rot;
        }

        if (!this.checkedFilter) {
            var parsedText = tmp.GetParsedText();
            if (parsedText is not null && parsedText != string.Empty) {
                // Filter without rich text tags, in case someone's witty enough to put a tag mid-profanity
                if (PlayerNameFilter.HitsFilter(parsedText)) {
                    tmp.text = Constants.CensoredName;
                }

                this.checkedFilter = true;
            }
        }
    }
}
