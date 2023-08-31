using System.Globalization;
using HarmonyLib;
using Reptile;
using SlopCrew.Common;
using UnityEngine;
using Player = Reptile.Player;

namespace SlopCrew.Plugin;

public class SlopScoreEncounter : Encounter {
    private float scoreGot;
    private AssociatedPlayer? opponent;
    private int opponentScore;
    private CultureInfo cultureInfo;

    public SlopScoreEncounter() {
        Core.OnUpdate += this.Update;
    }

    public override void InitSceneObject() {
        this.cultureInfo = CultureInfo.CurrentCulture;
        base.InitSceneObject();
    }

    public void StartMainEvent(uint encounterStartPlayerID) {
        if (Plugin.PlayerManager.Players.TryGetValue(encounterStartPlayerID, out var associatedPlayer)) {
            this.opponent = associatedPlayer;
        }
        var player = WorldHandler.instance.GetCurrentPlayer();
        Traverse.Create(player).Field<float>("score").Value = 0f;
        this.scoreGot = 0.0f;
        this.cultureInfo = CultureInfo.CurrentCulture;
    }

    public void Update() {
        if (this.opponent is null) return;
        if (!this.opponent.IsValid()) {
            this.opponent = null;
            this.TurnOffScoreUI();
            return;
        }

        var score = Plugin.PlayerManager.LastScoreAndMultiplier.Item1;
        var baseScore = Plugin.PlayerManager.LastScoreAndMultiplier.Item2;
        var scoreMultiplier = Plugin.PlayerManager.LastScoreAndMultiplier.Item3;

        var opponentScore = this.opponent.Score;
        var opponentBaseScore = this.opponent.BaseScore;
        var opponentScoreMultiplier = this.opponent.Multiplier;

        this.scoreGot = score + baseScore * scoreMultiplier;
        this.opponentScore = opponentScore + opponentBaseScore * opponentScoreMultiplier;
        this.SetScoreUI();
    }

    public void SetScoreUI() {
        var uiManager = Core.Instance.UIManager;
        var gameplay = Traverse.Create(uiManager).Field<GameplayUI>("gameplay").Value;
        gameplay.challengeGroup.SetActive(true);
        gameplay.timeLimitLabel.text = "TIME LIMIT LABEL";
        gameplay.targetScoreLabel.text =
            FormattingUtility.FormatPlayerScore(CultureInfo.CurrentCulture, this.opponentScore);
        gameplay.totalScoreLabel.text = FormattingUtility.FormatPlayerScore(CultureInfo.CurrentCulture, this.scoreGot);
        gameplay.targetScoreTitleLabel.text = this.opponent!.SlopPlayer.Name;
        gameplay.totalScoreTitleLabel.text = PlayerNameFilter.DoFilter(Plugin.SlopConfig.Username.Value);
    }

    public void TurnOffScoreUI() {
        var uiManager = Core.Instance.UIManager;
        var gameplay = Traverse.Create(uiManager).Field<GameplayUI>("gameplay").Value;
        gameplay.challengeGroup.SetActive(false);
        gameplay.timeLimitLabel.text = "";
        gameplay.targetScoreLabel.text = "";
        gameplay.totalScoreLabel.text = "";
        gameplay.targetScoreTitleLabel.text = "";
        gameplay.totalScoreTitleLabel.text = "";
    }
}
