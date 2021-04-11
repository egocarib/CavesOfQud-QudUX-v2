using XRL.Core;
using XRL.World;
using XRL.World.Parts;
using ConsoleLib.Console;
using GameObject = XRL.World.GameObject;
using QudColorUtility = ConsoleLib.Console.ColorUtility;
using Color = UnityEngine.Color;

namespace QudUX.Utilities
{
    public class TileMaker
    {
        public string Tile;
        public string RenderString;
        public string BackgroundString;
        public char DetailColorChar;
        public char ForegroundColorChar;
        public char BackgroundColorChar;
        public ushort Attributes;
        public Color DetailColor;
        public Color ForegroundColor;
        public Color BackgroundColor;

        public bool IsValid()
        {
            return (!string.IsNullOrEmpty(this.Tile) || !string.IsNullOrEmpty(this.RenderString));
        }

        public TileMaker(string blueprintString)
        {
            GameObject go = null;
            if (GameObjectFactory.Factory.Blueprints.ContainsKey(blueprintString))
            {
                go = GameObjectFactory.Factory.CreateObject(blueprintString);
            }
            this.Initialize(go, false);
            if (go != null)
            {
                go.Destroy();
            }
        }

        public TileMaker(GameObject go)
        {
            this.Initialize(go);
        }

        public bool WriteTileToBuffer(ScreenBuffer buffer, bool darken = false, string fallbackOutput = null)
        {
            bool result = WriteTileToBuffer(buffer, buffer.X, buffer.Y, darken);
            if (result == true)
            {
                buffer.X += 1;
            }
            else if (!string.IsNullOrEmpty(fallbackOutput))
            {
                buffer.Write(fallbackOutput);
            }
            return result;
        }

        //writes a tile to a ScreenBuffer. You'll still need to draw the buffer yourself
        public bool WriteTileToBuffer(ScreenBuffer scrapBuffer, int x, int y, bool darken = false)
        {
            if (scrapBuffer == null || x < 0 || x >= scrapBuffer.Width || y < 0 || y >= scrapBuffer.Height)
            {
                return false;
            }
            scrapBuffer[x, y].SetBackground(this.BackgroundColorChar);
            scrapBuffer[x, y].SetForeground(darken ? 'K' : this.ForegroundColorChar);
            scrapBuffer[x, y].SetDetail(darken ? 'K' : this.DetailColorChar);
            if (!string.IsNullOrEmpty(this.Tile))
            {
                scrapBuffer[x, y].TileForeground = scrapBuffer[x, y].Foreground;
                scrapBuffer[x, y].TileBackground = scrapBuffer[x, y].Background;
                scrapBuffer[x, y].Tile = this.Tile;
            }
            else if (!string.IsNullOrEmpty(this.RenderString))
            {
                scrapBuffer[x, y].Clear();
                scrapBuffer[x, y].Char = this.RenderString[0];
            }
            else
            {
                return false;
            }
            return true;
        }

        //returns true if the tile is already applied at the specified screen coordinates
        //(can be used for efficiency to avoid unnecessary work if you're drawing every frame)
        public bool IsTileOnScreen(int x, int y)
        {
            if (x < 0 || x >= TextConsole.CurrentBuffer.Width || y < 0 || y >= TextConsole.CurrentBuffer.Height)
            {
                return false;
            }
            ConsoleChar screenChar = TextConsole.CurrentBuffer[x, y];
            bool tileApplied = screenChar != null
                && screenChar.Foreground == this.ForegroundColor
                && screenChar.Background == this.BackgroundColor
                && ((!string.IsNullOrEmpty(this.Tile)
                        && screenChar.Tile == this.Tile
                        && screenChar.Detail == this.DetailColor
                        && screenChar.TileForeground == this.ForegroundColor
                        && screenChar.TileBackground == this.BackgroundColor)
                    || (!string.IsNullOrEmpty(this.RenderString)
                        && screenChar.Char == this.RenderString[0]));
            return tileApplied;
        }

        public bool IsTileOnScreen(Coords coords)
        {
            return this.IsTileOnScreen(coords.X, coords.Y);
        }

        private void Initialize(GameObject go, bool renderOK = true)
        {
            this.Tile = string.Empty;
            this.RenderString = string.Empty;
            this.BackgroundString = string.Empty;
            this.DetailColorChar = 'k';
            this.ForegroundColorChar = 'y';
            this.BackgroundColorChar = 'k';
            this.DetailColor = QudColorUtility.ColorMap['k'];
            this.ForegroundColor = QudColorUtility.ColorMap['y'];
            this.BackgroundColor = QudColorUtility.ColorMap['k'];

            //gather render data for GameObject similar to how the game does it in Cell.cs
            Render pRender = go?.pRender;
            if (pRender == null || !pRender.Visible || Globals.RenderMode != RenderModeType.Tiles)
            {
                return;
            }
            RenderEvent renderData = new RenderEvent();
            Examiner examinerPart = go.GetPart<Examiner>();
            if (examinerPart != null && !string.IsNullOrEmpty(examinerPart.UnknownTile) && !go.Understood())
            {
                renderData.Tile = examinerPart.UnknownTile;
            }
            else
            {
                renderData.Tile = go.pRender.Tile;
            }
            if (!string.IsNullOrEmpty(pRender.TileColor))
            {
                renderData.ColorString = pRender.TileColor;
            }
            else
            {
                renderData.ColorString = pRender.ColorString;
            }
            if (renderOK) //we can't render blueprint-created objects, because the game will throw errors trying to check their current cell
            {
                go.Render(renderData);
            }

            //renderData.Tile can be null if something has a temporary character replacement, like the up arrow from flying
            this.Tile = !string.IsNullOrEmpty(renderData.Tile) ? renderData.Tile : pRender.Tile;
            this.RenderString = !string.IsNullOrEmpty(renderData.RenderString) ? renderData.RenderString : pRender.RenderString;
            this.BackgroundString = renderData.BackgroundString;

            //save render data in our custom TileColorData format, using logic similar to QudItemListElement.InitFrom()
            if (!string.IsNullOrEmpty(pRender.DetailColor))
            {
                this.DetailColor = QudColorUtility.ColorMap[pRender.DetailColor[0]];
                this.DetailColorChar = pRender.DetailColor[0];
            }
            //from what I've been able to determine, I believe that the BackgroundString only applies to non-tiles (RenderString) entities (such as gas clouds)
            string colorString = renderData.ColorString + (string.IsNullOrEmpty(this.Tile) ? this.BackgroundString : string.Empty);
            if (!string.IsNullOrEmpty(colorString))
            {
                for (int j = 0; j < colorString.Length; j++)
                {
                    if (colorString[j] == '&' && j < colorString.Length - 1)
                    {
                        if (colorString[j + 1] == '&')
                        {
                            j++;
                        }
                        else
                        {
                            this.ForegroundColor = QudColorUtility.ColorMap[colorString[j + 1]];
                            this.ForegroundColorChar = colorString[j + 1];
                        }
                    }
                    if (colorString[j] == '^' && j < colorString.Length - 1)
                    {
                        if (colorString[j + 1] == '^')
                        {
                            j++;
                        }
                        else
                        {
                            this.BackgroundColor = QudColorUtility.ColorMap[colorString[j + 1]];
                            this.BackgroundColorChar = colorString[j + 1];
                        }
                    }
                }
            }
            this.Attributes = QudColorUtility.MakeColor(QudColorUtility.CharToColorMap[this.ForegroundColorChar], QudColorUtility.CharToColorMap[this.BackgroundColorChar]);
        }
    }
}