using SlopCrew.Common;
using TMPro;
using UnityEngine;

namespace SlopCrew.Plugin.UI;

public class TextMeshProFilter : MonoBehaviour {
    private TMP_Text tmp = null!;
    private bool checkedFilter;

    private void Awake() {
        this.tmp = this.gameObject.GetComponent<TMP_Text>();
    }

    private void Update() {
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
