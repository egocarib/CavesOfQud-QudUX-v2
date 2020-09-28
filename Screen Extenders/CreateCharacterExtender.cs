using XRL.World;
using ConsoleLib.Console;
using XRL.UI;
using System;
using QudUX.Utilities;

namespace QudUX.ScreenExtenders
{
    class CreateCharacterExtender
    {
        public static CharacterTileScreenExtender.TileMetadata TileInfo;
        public static GameObject TargetObject;
        public static ScreenBuffer CreateCharacterBuffer;
        public static Coords CustomTileWriteCoords;

        public static void ResetTileInfo()
        {
            TileInfo = null;
            TargetObject = null;
            CreateCharacterBuffer = null;
            CustomTileWriteCoords = null;
        }

        public static void ApplyTileInfoDeferred()
        {
            if (TileInfo != null && TargetObject != null)
            {
                ApplyTileInfoToObject(TileInfo, TargetObject);
            }
        }

        public static void ApplyTileInfoToObject(CharacterTileScreenExtender.TileMetadata tileInfo, GameObject target)
        {
            try
            {
                //for new game, these details gets applied later when patch calls ApplyTileInfoDeferred()
                TileInfo = tileInfo;
                TargetObject = target;

                //for other cases (such as opening a wish menu in game), the following applies changes immediately:
                target.pRender.Tile = tileInfo.Tile;
                target.pRender.DetailColor = tileInfo.DetailColor;
                target.pRender.TileColor = $"&{tileInfo.ForegroundColor}";
                target.pRender.ColorString = $"&{tileInfo.ForegroundColor}";

                //updates the Character Creation Complete screen buffer to show the new tile
                if (CreateCharacterBuffer != null && CustomTileWriteCoords != null && TargetObject != null)
                {
                    CreateCharacterBuffer.Goto(CustomTileWriteCoords.X, CustomTileWriteCoords.Y);
                    CreateCharacterBuffer.Write("{{y|[}}");
                    CreateCharacterBuffer.Write(TargetObject.pRender);
                    CreateCharacterBuffer.Write("{{y|]}}");
                }
            }
            catch (Exception ex)
            {
                Utilities.Logger.Log($"(Error) Failed to apply custom tile to body [{ex}]");
            }
        }

        public static void WriteCharCreateSpriteOptionText(ScreenBuffer buffer)
        {
            int row = 22;
            if (GameObjectFactory.Factory.GetBlueprintsWithTag("StartingPet").Count > 0)
            {
                row++;
            }
            buffer.Goto(17, row);
            buffer.Write("{{y|{{W|M}} - Modify character sprite}}");
            CreateCharacterBuffer = buffer;
            CustomTileWriteCoords = new Coords(buffer.X + 1, buffer.Y);
        }

        public static void PickCharacterTile(CharacterTemplate playerTemplate)
        {
            new QudUX_CharacterTileScreen().Show(playerTemplate);
        }
    }
}
