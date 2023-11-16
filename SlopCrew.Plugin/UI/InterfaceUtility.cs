using Microsoft.Extensions.Hosting;
using Reptile;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace SlopCrew.Plugin.UI;

public class InterfaceUtility(Config config) : IHostedService {
    public TMP_FontAsset? NameplateFont { get; private set; }
    public Material? NameplateFontMaterial { get; private set; }
    public Sprite? HeatStar { get; private set; }

    private static readonly Color NamePlateOutlineColor = new Color(0.1f, 0.1f, 0.1f, 1.0f);

    public void LoadAssets() {
        var assets = Core.Instance.Assets;
        assets.LoadAssetBundleByName("in_game_assets");

        var gameplayUIroot = assets.LoadAssetFromBundle<GameObject>("in_game_assets", "gameplayui");
        var gameplayUI = gameplayUIroot.GetComponentInChildren<GameplayUI>(true);

        NameplateFont = gameplayUI.trickNameLabel.font;
        NameplateFontMaterial = gameplayUI.trickNameLabel.fontMaterial;
        if (config.General.OutlineNameplates.Value) {
            NameplateFontMaterial.EnableKeyword(ShaderUtilities.Keyword_Outline);
            NameplateFontMaterial.SetColor(ShaderUtilities.ID_OutlineColor, NamePlateOutlineColor);
            NameplateFontMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.1f);

            NameplateFontMaterial.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            NameplateFontMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, NamePlateOutlineColor);
            NameplateFontMaterial.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.1f);
            NameplateFontMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.2f);
            NameplateFontMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.2f);
            NameplateFontMaterial.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.0f);
        }

        HeatStar = gameplayUI.wanted1.GetComponent<UnityEngine.UI.Image>().sprite;

        assets.UnloadAssetBundleByName("in_game_assets");
    }

    public Task StartAsync(CancellationToken cancellationToken) {
        Core.OnCoreInitialized += LoadAssets;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) {
        Core.OnCoreInitialized -= LoadAssets;
        return Task.CompletedTask;
    }
}
