

using SlopCrew.Common.Proto;
using UnityEngine;

namespace SlopCrew.Plugin.UI.Phone;

public class AppSpriteSheet {

    public static readonly Vector2Int SpriteSheetSize = new Vector2Int(1024, 1024);
    private const float SpriteSheetPadding = 16.0f;

    public static readonly Vector2 CategoryButtonSize = new Vector2(412.0f, 64.0f);
    public static readonly Vector2 CategoryIconSize = new Vector2(76.0f, 64.0f);
    public static readonly Vector2 EncounterButtonSize = new Vector2(512.0f, 167.0f);
    public static readonly Vector2 EncounterIconSize = new Vector2(116.0f, 101.0f);
    public static readonly Vector2 ChatButtonSize = new Vector2(427.0f, 62.0f);
    private static readonly float CategorySpriteHeight = SpriteSheetSize.y - (ChatButtonSize.y + SpriteSheetPadding) * 2.0f - CategoryButtonSize.y;
    private static readonly Vector2 CategoryIconGridStart = new(612.0f, SpriteSheetSize.y - 316.0f - CategoryButtonSize.y);
    private static readonly float EncounterIconGridHeight = SpriteSheetSize.y - (EncounterButtonSize.y + SpriteSheetPadding) * 2.0f - EncounterIconSize.y;
    private const int EncounterIconColumnCount = 4;
    private const int CategoryIconColumnCount = 4;

    public Sprite EncounterButtonSpriteNormal { get; private set; }
    public Sprite EncounterButtonSpriteSelected { get; private set; }
    public Sprite ChatSpriteNormal { get; private set; }
    public Sprite ChatSpriteSelected { get; private set; }
    public Sprite CategoryButtonNormal { get; private set; }
    public Sprite CategoryButtonSelected { get; private set; }

    private Sprite?[] encounterIcons;
    private Sprite?[] categoryIcons;

    public AppSpriteSheet(int encounterCount, int categoryCount) {

        var spriteSheet =
            TextureLoader.LoadResourceAsTexture("SlopCrew.Plugin.res.phone_app_sheet.png", SpriteSheetSize.x, SpriteSheetSize.y, false, TextureWrapMode.Clamp);

        var centerPivot = new Vector2(0.5f, 0.5f);

        // Load category screen sprites
        this.CategoryButtonNormal =
            Sprite.Create(spriteSheet, new Rect(SpriteSheetSize.x - CategoryButtonSize.x, CategorySpriteHeight, CategoryButtonSize.x, CategoryButtonSize.y),
                          centerPivot, 100.0f);
        this.CategoryButtonSelected =
            Sprite.Create(spriteSheet, new Rect(SpriteSheetSize.x - CategoryButtonSize.x, CategorySpriteHeight - CategoryButtonSize.y - SpriteSheetPadding, CategoryButtonSize.x, CategoryButtonSize.y),
                  centerPivot, 100.0f);

        categoryIcons = new Sprite[categoryCount];
        var categoryIconWidthPadded = CategoryIconSize.x + SpriteSheetPadding;
        var categoryIconHeightPadded = CategoryIconSize.y + SpriteSheetPadding;
        int categoryRow = -1;
        for (int i = 0; i < categoryCount; i++) {
            int column = i % CategoryIconColumnCount;
            if (column == 0) {
                categoryRow++;
            }

            categoryIcons[i] =
                Sprite.Create(spriteSheet, new Rect(CategoryIconGridStart.x + (categoryIconWidthPadded * column), CategoryIconGridStart.y - (categoryIconHeightPadded * categoryRow), CategoryIconSize.x, CategoryIconSize.y),
                              centerPivot, 100.0f);
        }

        // Load encounter sprites
        this.EncounterButtonSpriteSelected =
            Sprite.Create(spriteSheet, new Rect(0.0f, SpriteSheetSize.y - EncounterButtonSize.y, EncounterButtonSize.x, EncounterButtonSize.y),
                          centerPivot, 100.0f);
        this.EncounterButtonSpriteNormal =
            Sprite.Create(spriteSheet, new Rect(0.0f, SpriteSheetSize.y - (EncounterButtonSize.y * 2.0f) - SpriteSheetPadding, EncounterButtonSize.x, EncounterButtonSize.y),
                          centerPivot, 100.0f);

        encounterIcons = new Sprite[encounterCount];
        var encounterIconWidthPadded = EncounterIconSize.x + SpriteSheetPadding;
        var encounterIconHeightPadded = EncounterIconSize.y + SpriteSheetPadding;
        int encounterRow = -1;
        for (int i = 0; i < encounterCount; i++) {
            int column = i % EncounterIconColumnCount;
            if (column == 0) {
                encounterRow++;
            }

            encounterIcons[i] =
                Sprite.Create(spriteSheet, new Rect(encounterIconWidthPadded * column, EncounterIconGridHeight - (encounterIconHeightPadded * encounterRow), EncounterIconSize.x, EncounterIconSize.y),
                              centerPivot, 100.0f);
        }

        // Load chat sprites
        this.ChatSpriteSelected =
            Sprite.Create(spriteSheet, new Rect(SpriteSheetSize.x - ChatButtonSize.x, SpriteSheetSize.y - ChatButtonSize.y, ChatButtonSize.x, ChatButtonSize.y),
                          centerPivot, 100.0f);
        this.ChatSpriteNormal =
            Sprite.Create(spriteSheet, new Rect(SpriteSheetSize.x - ChatButtonSize.x, SpriteSheetSize.y - (ChatButtonSize.y * 2.0f) - SpriteSheetPadding, ChatButtonSize.x, ChatButtonSize.y),
              centerPivot, 100.0f);
    }

    public Sprite? GetEncounterIcon(EncounterType encounter) {
        return encounterIcons[(int) encounter];
    }
    public Sprite? GetCategoryIcon(AppSlopCrew.Category category) {
        return categoryIcons[(int) category];
    }
}
