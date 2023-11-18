

using SlopCrew.Common.Proto;
using UnityEngine;

namespace SlopCrew.Plugin.UI.Phone;

public class AppSpriteSheet {

    public static readonly Vector2Int SpriteSheetSize = new Vector2Int(1024, 1024);
    private const float SpriteSheetPadding = 16.0f;

    public static readonly Vector2 ButtonSpriteSize = new Vector2(512.0f, 167.0f);
    public static readonly Vector2 IconSpriteSize = new Vector2(116.0f, 101.0f);
    public static readonly Vector2 ChatSpriteSize = new Vector2(427.0f, 62.0f);
    private static readonly float IconSpriteGridHeight = SpriteSheetSize.y - (ButtonSpriteSize.y + SpriteSheetPadding) * 2.0f - IconSpriteSize.y;
    private const int IconGridColumns = 4;

    public Sprite EncounterButtonSpriteNormal { get; private set; }
    public Sprite EncounterButtonSpriteSelected { get; private set; }
    public Sprite ChatSpriteNormal { get; private set; }
    public Sprite ChatSpriteSelected { get; private set; }

    private Sprite?[] encounterIconSprites;

    public AppSpriteSheet(int encounterCount) {

        var spriteSheet =
            TextureLoader.LoadResourceAsTexture("SlopCrew.Plugin.res.phone_app_sheet.png", SpriteSheetSize.x, SpriteSheetSize.y, false, TextureWrapMode.Clamp);

        var centerPivot = new Vector2(0.5f, 0.5f);

        // Load main app sprites
        this.EncounterButtonSpriteSelected =
            Sprite.Create(spriteSheet, new Rect(0.0f, SpriteSheetSize.y - ButtonSpriteSize.y, ButtonSpriteSize.x, ButtonSpriteSize.y),
                          centerPivot, 100.0f);
        this.EncounterButtonSpriteNormal =
            Sprite.Create(spriteSheet, new Rect(0.0f, SpriteSheetSize.y - (ButtonSpriteSize.y * 2.0f) - SpriteSheetPadding, ButtonSpriteSize.x, ButtonSpriteSize.y),
                          centerPivot, 100.0f);

        encounterIconSprites = new Sprite[encounterCount];
        var iconWidthPadded = IconSpriteSize.x + SpriteSheetPadding;
        var iconHeightPadded = IconSpriteSize.y + SpriteSheetPadding;
        int row = -1;
        for (int i = 0; i < encounterCount; i++) {
            int column = i % IconGridColumns;
            if (column == 0) {
                row++;
            }

            encounterIconSprites[i] =
                Sprite.Create(spriteSheet, new Rect(iconWidthPadded * column, IconSpriteGridHeight - (iconHeightPadded * row), IconSpriteSize.x, IconSpriteSize.y),
                              centerPivot, 100.0f);
        }

        // Load chat sprites
        this.ChatSpriteSelected =
            Sprite.Create(spriteSheet, new Rect(SpriteSheetSize.x - ChatSpriteSize.x, SpriteSheetSize.y - ChatSpriteSize.y, ChatSpriteSize.x, ChatSpriteSize.y),
                          centerPivot, 100.0f);
        this.ChatSpriteNormal =
            Sprite.Create(spriteSheet, new Rect(SpriteSheetSize.x - ChatSpriteSize.x, SpriteSheetSize.y - (ChatSpriteSize.y * 2.0f) - SpriteSheetPadding, ChatSpriteSize.x, ChatSpriteSize.y),
              centerPivot, 100.0f);
    }

    public Sprite? GetEncounterIcon(EncounterType encounter) {
        return encounterIconSprites[(int) encounter];
    }
}
