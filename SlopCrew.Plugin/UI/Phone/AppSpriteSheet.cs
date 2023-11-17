

using SlopCrew.Common.Proto;
using UnityEngine;

namespace SlopCrew.Plugin.UI.Phone {
    public class AppSpriteSheet {

        public static readonly Vector2Int SpriteSheetSize = new Vector2Int(1024, 1024);
        private const float SpriteSheetPadding = 16.0f;

        public static readonly Vector2 ButtonSpriteSize = new Vector2(512.0f, 167.0f);
        public static readonly Vector2 IconSpriteSize = new Vector2(116.0f, 101.0f);
        public static readonly float IconSpriteGridHeight = SpriteSheetSize.y - (ButtonSpriteSize.y + SpriteSheetPadding) * 2.0f - IconSpriteSize.y;

        public Sprite? ButtonSpriteNormal { get; private set; }
        public Sprite? ButtonSpriteSelected { get; private set; }

        private Sprite?[] encounterIconSprites;

        public AppSpriteSheet(int encounterCount) {
            encounterIconSprites = new Sprite[encounterCount];

            var spriteSheet =
                TextureLoader.LoadResourceAsTexture("SlopCrew.Plugin.res.phone_app_sheet.png", SpriteSheetSize.x, SpriteSheetSize.y, false, TextureWrapMode.Clamp);

            var centerPivot = new Vector2(0.5f, 0.5f);

            // Load main app sprites
            this.ButtonSpriteSelected =
                Sprite.Create(spriteSheet, new Rect(0.0f, SpriteSheetSize.y - ButtonSpriteSize.y, ButtonSpriteSize.x, ButtonSpriteSize.y),
                              centerPivot, 100.0f);
            this.ButtonSpriteNormal =
                Sprite.Create(spriteSheet, new Rect(0.0f, SpriteSheetSize.y - (ButtonSpriteSize.y * 2.0f) - SpriteSheetPadding, ButtonSpriteSize.x, ButtonSpriteSize.y),
                              centerPivot, 100.0f);

            var iconSizeWithPadding = IconSpriteSize.x + SpriteSheetPadding;
            this.encounterIconSprites[0] =
                Sprite.Create(spriteSheet, new Rect(0.0f, IconSpriteGridHeight, IconSpriteSize.x, IconSpriteSize.y),
                              centerPivot, 100.0f);
            this.encounterIconSprites[1] =
                Sprite.Create(spriteSheet, new Rect(iconSizeWithPadding, IconSpriteGridHeight, IconSpriteSize.x, IconSpriteSize.y),
                              centerPivot, 100.0f);
            this.encounterIconSprites[2] =
                Sprite.Create(spriteSheet, new Rect(iconSizeWithPadding * 2.0f, IconSpriteGridHeight, IconSpriteSize.x, IconSpriteSize.y),
                              centerPivot, 100.0f);

            // Load chat sprites
        }

        public Sprite? GetEncounterIcon(EncounterType encounter) {
            return encounterIconSprites[(int) encounter];
        }
    }
}
