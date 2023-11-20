using Microsoft.Extensions.Hosting;
using Reptile;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore;

namespace SlopCrew.Plugin.UI;

public class InterfaceUtility(Config config) : IHostedService {
    public TMP_FontAsset? NameplateFont { get; private set; }
    public TMP_FontAsset? QuickChatFont { get; private set; }
    public Material? NameplateFontMaterial { get; private set; }
    public TMP_SpriteAsset? EmojiAsset { get; private set; }

    public static readonly Color NamePlateOutlineColor = new Color(0.1f, 0.1f, 0.1f, 1.0f);

    public void LoadAssets() {
        var assets = Core.Instance.Assets;
        assets.LoadAssetBundleByName("in_game_assets");

        var gameplayUIroot = assets.LoadAssetFromBundle<GameObject>("in_game_assets", "gameplayui");
        var gameplayUI = gameplayUIroot.GetComponentInChildren<GameplayUI>(true);

        this.QuickChatFont = gameplayUI.scoreTrickLabel.font;

        {
            this.NameplateFont = gameplayUI.trickNameLabel.font;
            this.NameplateFontMaterial = gameplayUI.trickNameLabel.fontMaterial;
            if (config.General.OutlineNameplates.Value) {
                this.NameplateFontMaterial.EnableKeyword(ShaderUtilities.Keyword_Outline);
                this.NameplateFontMaterial.SetColor(ShaderUtilities.ID_OutlineColor, NamePlateOutlineColor);
                this.NameplateFontMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.1f);

                this.NameplateFontMaterial.EnableKeyword(ShaderUtilities.Keyword_Underlay);
                this.NameplateFontMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, NamePlateOutlineColor);
                this.NameplateFontMaterial.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.1f);
                this.NameplateFontMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.2f);
                this.NameplateFontMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.2f);
                this.NameplateFontMaterial.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.0f);
            }
        }

        {
            const int spriteWidth = 512;
            const int spriteHeight = 512;
            const int spriteSheetRows = 4;
            const int spriteSheetCols = 4;
            const int spriteSheetPadding = 64;
            const float spriteScale = 1.5f;

            const int spriteSheetWidth = (spriteWidth * spriteSheetCols) + spriteSheetPadding;
            const int spriteSheetHeight = (spriteHeight * spriteSheetRows) + spriteSheetPadding;
            const int halfSpriteSheetPadding = spriteSheetPadding / 2;
            const float spriteWidthPercent = (float) spriteWidth / spriteSheetWidth;
            const float spriteHeightPercent = (float) spriteHeight / spriteSheetHeight;
            const float halfPaddingWidthPercent = (float) halfSpriteSheetPadding / spriteSheetWidth;
            const float halfPaddingHeightPercent = (float) halfSpriteSheetPadding / spriteSheetHeight;

            this.EmojiAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
            var texture =
                TextureLoader.LoadResourceAsTexture("SlopCrew.Plugin.res.emojis.png", spriteSheetWidth,
                                                    spriteSheetHeight);

            // NRE without this
            var material = new Material(Shader.Find("TextMeshPro/Sprite"));
            material.mainTexture = texture;
            this.EmojiAsset.material = material;

            this.EmojiAsset.spriteSheet = texture;
            this.EmojiAsset.spriteInfoList = new();
            this.EmojiAsset.m_Version = "1.1.0"; // Version migration is broken lol

            for (var y = 0; y < spriteSheetRows; y++) {
                for (var x = 0; x < spriteSheetCols; x++) {
                    var rect = new Rect(
                        halfPaddingWidthPercent + (x * spriteWidthPercent),
                        halfPaddingHeightPercent + (y * spriteHeightPercent),
                        spriteWidthPercent,
                        spriteHeightPercent
                    );
                    var pivot = new Vector2(0, 0);
                    var sprite = Sprite.Create(
                        texture,
                        rect,
                        pivot
                    );

                    var idx = (y * spriteSheetCols) + x;
                    var unicode = 0x1F600 + idx;
                    var spriteX = (x * spriteWidth) + halfSpriteSheetPadding;
                    var spriteY = spriteSheetHeight - (y * spriteHeight) - spriteHeight - halfSpriteSheetPadding;

                    var tmpSprite = new TMP_Sprite {
                        id = idx,
                        name = $"{x}_{y}",
                        unicode = unicode,
                        sprite = sprite,
                        pivot = pivot,
                        width = spriteWidth,
                        height = spriteHeight,
                        x = spriteX,
                        y = spriteY,
                        scale = spriteScale
                    };
                    this.EmojiAsset.spriteInfoList.Add(tmpSprite);

                    // no idea why i need the offset here but it aligns it better
                    var metrics = new GlyphMetrics(spriteWidth, spriteHeight,
                                                   0, spriteHeight - spriteSheetPadding,
                                                   spriteWidth);
                    var glyphRect = new GlyphRect(spriteX, spriteY, spriteWidth, spriteHeight);
                    var glyph = new TMP_SpriteGlyph((uint) idx, metrics, glyphRect, spriteScale, 0);
                    this.EmojiAsset.spriteGlyphTable.Add(glyph);

                    var character = new TMP_SpriteCharacter((uint) unicode, this.EmojiAsset, glyph) {
                        glyphIndex = (uint) idx
                    };
                    this.EmojiAsset.spriteCharacterTable.Add(character);
                }
            }

            this.EmojiAsset.UpdateLookupTables();
        }

        assets.UnloadAssetBundleByName("in_game_assets");
    }

    public Task StartAsync(CancellationToken cancellationToken) {
        Core.OnCoreInitialized += this.LoadAssets;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) {
        Core.OnCoreInitialized -= this.LoadAssets;
        return Task.CompletedTask;
    }
}
