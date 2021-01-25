using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using XRL;
using XRL.UI;
using XRL.World;
using XRL.World.Parts;
using ConsoleLib.Console;
using QudUX.Utilities;
using static QudUX.Utilities.Logger;
using QudUX.Concepts;

namespace QudUX.ScreenExtenders
{
    class CharacterTileScreenExtender
    {
        public readonly GameObject TargetBody;
        public List<TileMetadata> Tiles;
        public int SelectedTileIndex = 0;
        public int DrawIndex = 0;
        public int DrawPositionX = -1;
        public int DrawPositionY = -1;
        public int DrawBoundaryX1 = 0;
        public int DrawBoundaryX2 = 0;
        public int DrawBoundaryY1 = 0;
        public int DrawBoundaryY2 = 0;
        public int DrawIncrementX = 1;
        public int DrawIncrementY = 1;
        public char DefaultForegroundColor;
        public bool IsPhotosynthetic;
        public string CurrentQuery { get; set; }

        public CharacterTileScreenExtender(CharacterTemplate playerTemplate)
        {
            SetDefaultColors(playerTemplate.PlayerBody);
            InitCoreTiles(playerTemplate);
            TargetBody = playerTemplate.PlayerBody;
        }

        public CharacterTileScreenExtender(GameObject targetBody, List<string> blueprintParents)
        {
            SetDefaultColors(targetBody);
            InitBlueprintTiles(blueprintParents);
            TargetBody = targetBody;
        }

        public CharacterTileScreenExtender(GameObject targetBody, string blueprintQuerystring)
        {
            blueprintQuerystring = NormalizeQuerystring(blueprintQuerystring);
            SetDefaultColors(targetBody);
            InitBlueprintTiles(blueprintQuerystring);
            CurrentQuery = blueprintQuerystring;
            TargetBody = targetBody;
        }

        public string NormalizeQuerystring(string querystring)
        {
            if (querystring == null)
            {
                return string.Empty;
            }
            return querystring.Trim().ToLower();
        }

        public bool CurrentQueryEquals(string queryToCheck)
        {
            return CurrentQuery == NormalizeQuerystring(queryToCheck);
        }

        public void SetDefaultColors(GameObject targetBody)
        {
            //determine foreground color for tiles ('y' unless player has Photosynthetic skin, then 'g')
            DefaultForegroundColor = 'y';
            IsPhotosynthetic = false;
            Mutations mutations = targetBody.GetPart<Mutations>();
            if (mutations != null && mutations.MutationList.Count > 0)
            {
                for (int i = 0; i < mutations.MutationList.Count; i++)
                {
                    if (mutations.MutationList[i].DisplayName == "Photosynthetic Skin")
                    {
                        DefaultForegroundColor = 'g';
                        IsPhotosynthetic = true;
                        break;
                    }
                }
            }
        }

        public void InitCoreTiles(CharacterTemplate playerTemplate = null)
        {
            CurrentQuery = null;
            List<SubtypeEntry> gameSubtypes = new List<SubtypeEntry>();
            foreach (SubtypeClass subtypeClass in SubtypeFactory.Classes)
            {
                gameSubtypes.AddRange(subtypeClass.GetAllSubtypes());
            }
            Tiles = new List<TileMetadata>(gameSubtypes.Count);
            SelectedTileIndex = 0;
            foreach (SubtypeEntry subtype in gameSubtypes)
            {
                Tiles.Add(new TileMetadata(subtype.Tile, subtype.DetailColor, DefaultForegroundColor.ToString(), $"Subtype: {subtype.Name}", "Subtypes.xml"));
                if (playerTemplate?.Subtype != null && playerTemplate.Subtype == subtype.Name)
                {
                    SelectedTileIndex = this.Tiles.Count - 1;
                }
            }
        }

        public void InitBlueprintTiles(List<string> parentNodes)
        {
            IEnumerable<GameObjectBlueprint> matches = GameObjectFactory.Factory.BlueprintList
                .Where(bp => !bp.Tags.ContainsKey("BaseObject") && !bp.Name.EndsWith(" Cherub"))
                .Where(bp => 
                { 
                    foreach (string node in parentNodes)
                    {
                        if (bp.Name == node || bp.InheritsFrom(node))
                        {
                            return true;
                        }
                    }
                    return false;
                });
            Tiles = LoadBlueprintTiles(matches).OrderBy(tm => tm.Tile).ThenBy(tm => tm.ForegroundColorIndex).ToList();
        }

        public void InitBlueprintTiles(string query)
        {
            if (query == "cherubim")
            {
                var matches = GameObjectFactory.Factory.BlueprintList
                    .Where(bp => !bp.Tags.ContainsKey("BaseObject") && bp.Name.EndsWith(" Cherub"));
                Tiles = LoadBlueprintTiles(matches).OrderBy(tm => tm.Tile).ThenBy(tm => tm.ForegroundColorIndex).ToList();
            }
            else if (query.Length > 0)
            {
                var matches = GameObjectFactory.Factory.BlueprintList
                    .Where(bp => CheckBlueprintMatchesQuery(bp, query));
                Tiles = LoadBlueprintTiles(matches).OrderBy(tm => tm.Tile).ThenBy(tm => tm.ForegroundColorIndex).ToList();
            }
            else
            {
                Log($"(Error) Blank blueprint query '{query}'");
                Tiles = new List<TileMetadata>();
            }
        }

        public bool CheckBlueprintMatchesQuery(GameObjectBlueprint blueprint, string queryLowercase)
        {
            if (blueprint == null)
            {
                return false;
            }
            if (!string.IsNullOrEmpty(blueprint.Name) && blueprint.Name.ToLower().Contains(queryLowercase))
            {
                return true;
            }
            bool hasParent = !string.IsNullOrEmpty(blueprint.Inherits);
            if (hasParent && blueprint.Inherits.ToLower().Contains(queryLowercase))
            {
                return true;
            }
            GameObjectBlueprint parent;
            if (hasParent && GameObjectFactory.Factory.Blueprints.TryGetValue(blueprint.Inherits, out parent))
            {
                if (!string.IsNullOrEmpty(parent.Inherits) && parent.Inherits.ToLower().Contains(queryLowercase))
                {
                    return true;
                }
            }
            GamePartBlueprint renderPart = blueprint.GetPart("Render");
            if (renderPart != null)
            {
                string displayName = renderPart.Parameters.ContainsKey("DisplayName") ? renderPart.Parameters["DisplayName"] : null;
                if (!string.IsNullOrEmpty(displayName))
                {
                    if (ColorUtility.StripFormatting(displayName).ToLower().Contains(queryLowercase))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public IEnumerable<TileMetadata> LoadBlueprintTiles(IEnumerable<GameObjectBlueprint> blueprints)
        {
            HashSet<string> foundTiles = new HashSet<string>();
            foreach (GameObjectBlueprint blueprint in blueprints)
            {
                GamePartBlueprint renderPart = blueprint.GetPart("Render");
                if (renderPart == null)
                {
                    continue;
                }
                string detailColor = renderPart.Parameters.ContainsKey("DetailColor") ? renderPart.Parameters["DetailColor"] : null;
                string foregroundColor = renderPart.Parameters.ContainsKey("ColorString") ? renderPart.Parameters["ColorString"] : "&y";
                string tileColor = renderPart.Parameters.ContainsKey("TileColor") ? renderPart.Parameters["TileColor"] : null;
                if (!string.IsNullOrEmpty(tileColor))
                {
                    foregroundColor = tileColor; //Some things use TileColor attribute; it has the same structure as ColorString
                }
                if (string.IsNullOrEmpty(detailColor) || string.IsNullOrEmpty(foregroundColor) || detailColor.Length != 1)
                {
                    continue;
                }
                int idx = foregroundColor.IndexOf('&');
                if (idx >= 0 && idx + 1 < foregroundColor.Length)
                {
                    foregroundColor = foregroundColor[foregroundColor.LastIndexOf('&') + 1].ToString();
                }
                GamePartBlueprint holoPart = blueprint.GetPart("HologramMaterial");
                if (holoPart != null)
                {
                    foregroundColor = "B";
                    detailColor = "b";
                }
                GamePartBlueprint dischargePart = blueprint.GetPart("DischargeOnStep");
                if (dischargePart != null)
                {
                    foregroundColor = "W";
                }
                if (IsPhotosynthetic)
                {
                    foregroundColor = "g";
                }
                GamePartBlueprint randomTilePart = blueprint.GetPart("RandomTile");
                if (randomTilePart == null)
                {
                    string tilePath = renderPart.Parameters.ContainsKey("Tile") ? renderPart.Parameters["Tile"] : null;
                    tilePath = tilePath != null ? tilePath.ToLower() : null;
                    if (!string.IsNullOrEmpty(tilePath))
                    {
                        if (!foundTiles.Contains($"{tilePath}{detailColor}{foregroundColor}"))
                        {
                            foundTiles.Add($"{tilePath}{detailColor}{foregroundColor}");
                            string displayName = renderPart.Parameters.ContainsKey("DisplayName") ? renderPart.Parameters["DisplayName"] : "";
                            string blueprintPath = BlueprintPath(blueprint);
                            yield return new TileMetadata(tilePath, detailColor, foregroundColor, displayName, blueprintPath);
                        }
                    }
                }
                else //ignore Render Tile if this has RandomTile part (as the game does)
                {
                    string tileList = randomTilePart.Parameters.ContainsKey("Tiles") ? randomTilePart.Parameters["Tiles"] : null;
                    if (!string.IsNullOrEmpty(tileList))
                    {
                        string displayName = renderPart.Parameters.ContainsKey("DisplayName") ? renderPart.Parameters["DisplayName"] : "";
                        string blueprintPath = BlueprintPath(blueprint);
                        foreach (string tile in tileList.ToLower().Split(','))
                        {
                            if (!foundTiles.Contains($"{tile}{detailColor}{foregroundColor}"))
                            {
                                foundTiles.Add($"{tile}{detailColor}{foregroundColor}");
                                yield return new TileMetadata(tile, detailColor, foregroundColor, displayName, blueprintPath);
                            }
                        }
                    }
                }
            }
        }

        public string BlueprintPath(GameObjectBlueprint blueprint)
        {
            string path = string.Empty;
            if (blueprint == null)
            {
                return path;
            }
            if (!string.IsNullOrEmpty(blueprint.Name))
            {
                path = blueprint.Name;
            }
            bool hasParent = !string.IsNullOrEmpty(blueprint.Inherits);
            if (hasParent)
            {
                path = blueprint.Inherits + " > " + path;
            }
            GameObjectBlueprint parent;
            if (hasParent && GameObjectFactory.Factory.Blueprints.TryGetValue(blueprint.Inherits, out parent))
            {
                if (!string.IsNullOrEmpty(parent.Inherits))
                {
                    path = parent.Inherits + " > " + path;
                }
            }
            return path;
        }

        public string SelectionDisplayName
        {
            get
            {
                return (SelectedTileIndex < Tiles.Count) ? Tiles[SelectedTileIndex].DisplayName : string.Empty;
            }
        }

        public string SelectionBlueprintPath
        {
            get
            {
                return (SelectedTileIndex < Tiles.Count) ? Tiles[SelectedTileIndex].BlueprintPath : string.Empty;
            }
        }

        public void ResetDrawArea(int x1, int y1, int x2, int y2, int xIncrement = 2, int yIncrement = 2, int startingTileIndex = 0, bool preserveSelection = false)
        {
            SelectedTileIndex = preserveSelection ? SelectedTileIndex : 0;
            DrawIndex = startingTileIndex;
            DrawBoundaryX1 = x1;
            DrawBoundaryY1 = y1;
            DrawBoundaryX2 = x2;
            DrawBoundaryY2 = y2;
            DrawPositionX = x1;
            DrawPositionY = y1;
            DrawIncrementX = xIncrement;
            DrawIncrementY = yIncrement;
            Tiles.ForEach(tile => tile.DrawCoords = null);
        }

        public int DrawFillTiles(ScreenBuffer buffer)
        {
            buffer.Fill(DrawBoundaryX1, DrawBoundaryY1, DrawBoundaryX2, DrawBoundaryY2, ' ', ColorUtility.MakeColor(TextColor.Black, TextColor.Black));
            int count = 0;
            while (DrawNext(buffer))
            {
                count++;
            }
            DrawSelectionBox(buffer);
            return count;
        }

        public bool DrawNext(ScreenBuffer buffer)
        {
            if (DrawIndex >= Tiles.Count)
            {
                return false;
            }
            if (DrawPositionX > DrawBoundaryX2)
            {
                DrawPositionY += DrawIncrementY;
                DrawPositionX = DrawBoundaryX1;
            }
            if (DrawPositionY > DrawBoundaryY2)
            {
                return false;
            }

            buffer.Goto(DrawPositionX, DrawPositionY);

            buffer.CurrentChar.SetBackground('k');
            buffer.CurrentChar.SetForeground(Tiles[DrawIndex].ForegroundColor[0]);
            buffer.CurrentChar.TileBackground = ColorUtility.ColorMap[Tiles[DrawIndex].DetailColor[0]];
            buffer.CurrentChar.TileForeground = ColorUtility.ColorMap[Tiles[DrawIndex].ForegroundColor[0]];
            buffer.CurrentChar.Tile = Tiles[DrawIndex].Tile;

            Tiles[DrawIndex].DrawCoords = new Coords(DrawPositionX, DrawPositionY);
            if (DrawIndex == SelectedTileIndex)
            {
                DrawSelectionBox(buffer);
            }

            DrawIndex++;
            DrawPositionX += DrawIncrementX;
            return true;
        }

        //this is an older method that should be replaced by a call to ResetDrawArea & DrawFillTiles
        public void DrawTileLine(ScreenBuffer buffer)
        {
            if (Tiles.Count <= 0)
            {
                return;
            }

            for (int i = 0; i < Tiles.Count; i++)
            {
                if (buffer.X >= 80)
                {
                    break;
                }
                if (i == SelectedTileIndex)
                {
                    buffer.SingleBox(buffer.X - 1, buffer.Y - 1, buffer.X + 1, buffer.Y + 1, ColorUtility.MakeColor(TextColor.Grey, TextColor.Black));
                }

                Tiles[i].DrawCoords = new Coords(buffer.X, buffer.Y);

                buffer.CurrentChar.SetBackground('k');
                buffer.CurrentChar.SetForeground(Tiles[i].ForegroundColor[0]);
                buffer.CurrentChar.TileBackground = ColorUtility.ColorMap[Tiles[i].DetailColor[0]];
                buffer.CurrentChar.TileForeground = ColorUtility.ColorMap[Tiles[i].ForegroundColor[0]];
                buffer.CurrentChar.Tile = Tiles[i].Tile;
                buffer.X += 2;
            }
        }

        public void EraseSelectionBox(ScreenBuffer buffer)
        {
            DrawSelectionBox(buffer, true);
        }

        public void DrawSelectionBox(ScreenBuffer buffer, bool bErase = false)
        {
            if (Tiles != null && Tiles.Count > SelectedTileIndex)
            {
                Coords coords = Tiles[SelectedTileIndex].DrawCoords;
                if (coords != null)
                {
                    ushort color = ColorUtility.MakeColor(bErase ? TextColor.Black : TextColor.Grey, TextColor.Black);
                    buffer.SingleBox(coords.X - 1, coords.Y - 1, coords.X + 1, coords.Y + 1, color);
                }
            }
        }

        public void RotateDetailColor(int amount = 1)
        {
            Tiles[SelectedTileIndex].DetailColorIndex += amount;
        }

        public void RotateForegroundColor(int amount = 1)
        {
            Tiles[SelectedTileIndex].ForegroundColorIndex += amount;
        }

        public void Flip()
        {
            Tiles[SelectedTileIndex].Flip();
        }

        public void MoveSelection(Keys key)
        {
            Coords curCoords = Tiles[SelectedTileIndex].DrawCoords;
            TileMetadata destinationTile = null;
            if (key == Keys.NumPad2)
            {
                destinationTile = Tiles
                    .Where(t => t.DrawCoords != null && t.DrawCoords.X == curCoords.X && t.DrawCoords.Y > curCoords.Y)
                    .OrderBy(t => t.DrawCoords.Y).FirstOrDefault();
            }
            if (key == Keys.NumPad4)
            {
                destinationTile = Tiles
                    .Where(t => t.DrawCoords != null && t.DrawCoords.Y == curCoords.Y && t.DrawCoords.X < curCoords.X)
                    .OrderByDescending(t => t.DrawCoords.X).FirstOrDefault();
            }
            if (key == Keys.NumPad6)
            {
                destinationTile = Tiles
                    .Where(t => t.DrawCoords != null && t.DrawCoords.Y == curCoords.Y && t.DrawCoords.X > curCoords.X)
                    .OrderBy(t => t.DrawCoords.X).FirstOrDefault();
            }
            if (key == Keys.NumPad8)
            {
                destinationTile = Tiles
                    .Where(t => t.DrawCoords != null && t.DrawCoords.X == curCoords.X && t.DrawCoords.Y < curCoords.Y)
                    .OrderByDescending(t => t.DrawCoords.Y).FirstOrDefault();
            }
            if (destinationTile != null)
            {
                SelectedTileIndex = Tiles.FindIndex(t => t == destinationTile);
            }
        }

        public string CurrentTileForegroundColor()
        {
            string val = "y";
            try
            {
                TileMetadata tileInfo = Tiles[SelectedTileIndex];
                val = tileInfo.ForegroundColor;
            }
            catch (Exception ex)
            {
                Log($"(Error) Failed to retrieve selected tile foreground color [{ex}]");
            }
            return val;
        }

        public void ApplyToTargetBody()
        {
            TileMetadata tileInfo;
            try
            {
                tileInfo = Tiles[SelectedTileIndex];
            }
            catch (Exception ex)
            {
                Log($"(Error) Failed to retrieve tile info when attempting to apply tile customizations [{ex}]");
                return;
            }
            CreateCharacterExtender.ApplyTileInfoToObject(tileInfo, TargetBody);
        }

        public class TileMetadata
        {
            public Coords DrawCoords = null;
            private readonly static List<string> ColorList = new List<string>()
            {
                "K", "k", "Y", "y", "c", "C", "B", "b", "g", "G", "W", "w", "o", "O", "r", "R", "m", "M"
            };
            public int DetailColorIndex;
            public int ForegroundColorIndex;
            public string DisplayName;
            public string BlueprintPath;
            public bool IsFlipped;
            public readonly string _Tile;
            public readonly string _FlippedTile;
            public string Tile
            {
                get
                {
                    if (!IsFlipped)
                    {
                        return _Tile;
                    }
                    return _FlippedTile;
                }
            }
            public string DetailColor
            {
                get
                {
                    if (DetailColorIndex < 0)
                    {
                        DetailColorIndex = ColorList.Count - 1;
                    }
                    else if (DetailColorIndex >= ColorList.Count)
                    {
                        DetailColorIndex = 0;
                    }
                    return ColorList[DetailColorIndex];
                }
            }
            public string ForegroundColor
            {
                get
                {
                    if (ForegroundColorIndex < 0)
                    {
                        ForegroundColorIndex = ColorList.Count - 1;
                    }
                    else if (ForegroundColorIndex >= ColorList.Count)
                    {
                        ForegroundColorIndex = 0;
                    }
                    return ColorList[ForegroundColorIndex];
                }
            }

            public TileMetadata(string tile, string detailColor, string foregroundColor, string displayName, string blueprintPath)
            {
                _Tile = tile;
                _FlippedTile = $"{Path.ChangeExtension(_Tile, null)}{Constants.FlippedTileSuffix}";
                DetailColorIndex = ColorList.FindIndex(c => c == detailColor);
                ForegroundColorIndex = ColorList.FindIndex(c => c == foregroundColor);
                DisplayName = displayName;
                BlueprintPath = blueprintPath;
            }

            public void Flip()
            {
                IsFlipped = !IsFlipped;
            }
        }
    }
}
