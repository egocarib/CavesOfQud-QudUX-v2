
using ConsoleLib.Console;
using System.Collections.Generic;
using System.Linq;
using System;
using XRL.UI;

namespace QudUX.Utilities
{

    public class QudUXBorder
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public void Display(ScreenBuffer buffer)
        {
            ushort colorGrey = ColorUtility.MakeColor(TextColor.Grey, TextColor.Black);
            buffer[X,Y].Char = (char) 218; //  ┌
            buffer[X,Y].Attributes = colorGrey;
            buffer[X+Width-1,Y].Char = (char) 191; // ┐
            buffer[X+Width-1,Y].Attributes = colorGrey;
            buffer[X,Y+Height -1].Char = (char) 192; //  └
            buffer[X,Y+Height -1].Attributes = colorGrey;
            buffer[X+Width-1,Y+Height -1 ].Char = (char) 217; // ┘
            buffer[X+Width-1,Y+Height -1 ].Attributes = colorGrey;
            
            for (int borderx=X+1; borderx < X+Width - 1 ;borderx++)
            {
                    buffer[borderx, Y].Char = (char)196;       //  ─
                    buffer[borderx, Y].Attributes = colorGrey;
                    buffer[borderx, Y+Height-1].Char = (char)196;       //  ─
                    buffer[borderx, Y+Height-1].Attributes = colorGrey;
            }
            for (int bordery=Y+1; bordery < Y+Height - 1 ; bordery++) 
            {
                    buffer[X, bordery].Char = ((char)179);  // │
                    buffer[X, bordery].Attributes = colorGrey;
                    buffer[X+Width - 1, bordery ].Char = ((char)179) ;  // │
                    buffer[X+Width -1 , bordery ].Attributes = colorGrey; 
            }

        }

        public QudUXBorder(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }
    public class QudUXTextBlock
    {
        public string Text { get => text; set  {text = value; textLines = null;} }
        private int x, y, width, height;
        private int offset = 0;
        public bool DrawBorder = false;

        private List<string> textLines;
        private string text;

        public QudUXTextBlock(int col, int lig, int ctlWidth, int ctlHeight)
        {
            x = col;
            y = lig;
            width = ctlWidth;
            height = ctlHeight;
        }

        public void Scroll(int direction, int lines)
        {
            if (textLines == null) return;

            offset += direction * lines;
            if (offset < 0)
                offset = 0;

            if (offset > textLines.Count - 1)
                offset = textLines.Count - 1;

        }
        public void Scroll(int direction)
        {
            Scroll(direction, 1);
        }

        public void ScrollPage(int direction)
        {
            int pagesize = height - (DrawBorder ? 2 : 0);
            Scroll(direction, pagesize);
        }
        private void FillText(string text)
        {
            textLines = new List<string>();
            var lines = text.Split('\n');
            int border = 0;
            if (DrawBorder)
            {
                border = 2;
            }

            for (int i = 0; i < lines.Count(); i++)
            {
                // remove formatting to count real number of characters
                string line = ColorUtility.StripFormatting(lines[i]);
                if (line.Length + border < width)
                {
                    textLines.Add(lines[i]);
                }
                else
                {
                    var formlines = StringFormat.ClipTextToArray(lines[i], width - border);
                    foreach (var l in formlines)
                    {
                        textLines.Add(l);
                    }
                }

            }
        }

        public void Display(ScreenBuffer buffer)
        {
            if (textLines == null) FillText(Text);
            ushort colorGrey = ColorUtility.MakeColor(TextColor.Grey, TextColor.Black);
            int i = 0;
            int borderSize = 0;
            int cury = y;
            int curx = x;
            if (DrawBorder)
            {
                QudUXBorder border = new QudUXBorder(x, y, width, height);
                border.Display(buffer);
                borderSize = 2;
                cury++;
                curx++;

            }
            int posTxt = textLines.Count - offset;
            int limit = Math.Min(posTxt, height - borderSize);
            for (i = offset; i < offset + limit; i++)
            {
                string l = textLines[i];
                buffer.Goto(curx, cury);
                buffer.Write(l);
                cury++;
            }


        }
    }
    public class Table
    {
        public class ColumnDefinition
        {
            public string Header { get; set; }
            public int Width { get; set; }
        }
        public int TableWidth { get; private set; }
        public int MaxVisibleRows  { get; set; } = 18;
        public int CurrentVisibleRows  { get ; private set ; } 
        public bool ShowHeader { get ; private set ; }  = true;
        private int selectedIndex;
        public int SelectedIndex
        {
            get { return selectedIndex; }
            set { selectedIndex = Math.Min(MaxVisibleRows - 1,value); }
        }
        public bool ShowSelection { get; set; } = true;

        public string HeaderTemplate { get; set; } = "W-R-W alternation";

        public int Offset { get; set; } = 0;

        private List<ColumnDefinition> columns;
        private List<List<string>> rows = new List<List<string>>();

        public List<ColumnDefinition> ColumnsDefinition
        {
            get => columns;
            set
            {
                columns = value;
                if (columns != null)
                {
                    TableWidth = (from c in columns select c.Width).Sum() + columns.Count  +1;
                }
            }
        }

        public List<List<string>> Rows { get => rows; set => rows = value; }

        public Table(List<ColumnDefinition> columns)
        {
            ColumnsDefinition = columns;
        }

        public void Display(ScreenBuffer screenBuffer, int x, int y)
        {
            int line = y;
            if (ShowHeader)
            {
                DisplayHeader(screenBuffer, x, y);
			    line += 3;
            }
            
            int rownum = 0;
            var visibleRows = rows.Skip(Offset).Take(MaxVisibleRows);
            CurrentVisibleRows = visibleRows.Count();
            if (SelectedIndex > CurrentVisibleRows)
                SelectedIndex = 0;
			foreach(List<string> row in visibleRows)
			{
                
                bool selected = ShowSelection && (rownum  == SelectedIndex);
            	DisplayRow(screenBuffer,row,x,line,selected);
				line++;
                rownum++;
			}

        }
        public void ScrollPage (int direction)
        {
            if ((Offset + (direction * MaxVisibleRows) < rows.Count) && (Offset + (direction * MaxVisibleRows) >= 0))
            {
                Offset += (direction * MaxVisibleRows);
            }
        }

        public void Scroll (int direction)
        {
            if ((Offset + direction < rows.Count) && (Offset + direction  >= 0))
            {
                Offset += direction ;
            }
        }

        public void MoveSelection (int direction)
        {
            if ((SelectedIndex + direction < CurrentVisibleRows) && (SelectedIndex + direction   >= 0))
            {
                SelectedIndex += direction ;
                return;
            }

            if (SelectedIndex + direction   < 0)
            {
                SelectedIndex = CurrentVisibleRows - 1;
                return; 
            }

            if (SelectedIndex + direction >= CurrentVisibleRows)
            {
                SelectedIndex = 0;
                return;
            }
        }

        private void DisplayHeader(ScreenBuffer screenBuffer, int line, int col)
        {
            ushort colorGrey = ColorUtility.MakeColor(TextColor.Grey, TextColor.Black);
            screenBuffer.SingleBox(col, line, TableWidth, line + 2, colorGrey);
            int pos = col;
			for(int colidx=0 ; colidx < columns.Count; colidx++)
			{
				ColumnDefinition cold = columns[colidx];
				screenBuffer.Goto(pos + 1, line+1);

                if (HeaderTemplate.Length > 0)				    
                    screenBuffer.Write("{{"+HeaderTemplate+"|" + cold.Header+"}}");
                else
                    screenBuffer.Write(cold.Header);

                pos = pos + cold.Width + 1;

				if (colidx < columns.Count -1)
                	screenBuffer.SingleBoxVerticalDivider(pos, line, line + 2);
                
            }
        }

        private void DisplayRow(ScreenBuffer screenBuffer, List<string> row ,int x, int y,bool selected)
        {
            int postxt = x+1;

			for(int col=0 ; col < columns.Count; col++)
			{
				ColumnDefinition cd = columns[col];
                string rowVal = row[col];
                // trim last column if necessary
                if (rowVal.Length > cd.Width)
                {
                    rowVal = rowVal.Substring(0,cd.Width-1);
                }

                if (selected && col == 0)
                {
                    rowVal =">" + rowVal;
                    screenBuffer.Goto(postxt-1, y);
                } 
                else
                {
                    screenBuffer.Goto(postxt, y);
                }

				screenBuffer.Write(rowVal);
				postxt += cd.Width + 1;
			}
        }


    }

    public static class ConsoleUtilities
    {
        public static void WriteLine(this ScreenBuffer buffer, string text)
        {
            int lineStart = buffer.X;
            buffer.Write(text);
            buffer.X = lineStart;
            buffer.Y++;
        }

        public static void Write(this ScreenBuffer buffer, int x, int y, string text, bool yFormatWrap = true)
        {
            if (yFormatWrap)
            {
                text = "{{y|" + text + "}}";
            }
            buffer.Goto(x, y);
            buffer.Write(text);
        }

        public static void SingleBox(this ScreenBuffer buffer, int x1 = 0, int y1 = 0, int x2 = 79, int y2 = 24)
        {
            buffer.SingleBox(x1, y1, x2, y2, ColorUtility.MakeColor(TextColor.Grey, TextColor.Black));
        }

        public static void TitledBox(this ScreenBuffer buffer, string title)
        {
            buffer.SingleBox();
            buffer.Title(title);
        }

        public static void Title(this ScreenBuffer buffer, string title, bool format = true)
        {
            if (format)
            {
                title = "{{y|[ {{W|" + title + "}} ]}}";
            }
            int startPos = (80 - ColorUtility.StripFormatting(title).Length) / 2;
            buffer.Goto(startPos, 0);
            buffer.Write(title);
        }

        public static void EscOr5ToExit(this ScreenBuffer buffer)
        {
            buffer.Goto(60, 0);
            buffer.Write("{{y| {{W|ESC}} or {{W|5}} to exit }}");
        }

        public static void EscOr5GoBack(this ScreenBuffer buffer)
        {
            buffer.Goto(60, 0);
            buffer.Write("{{y| {{W|ESC}} or {{W|5}} go back }}");
        }

        public static void SingleBoxHorizontalDivider(this ScreenBuffer buffer, int y)
        {
            ushort colorGrey = ColorUtility.MakeColor(TextColor.Grey, TextColor.Black);

            for (int x = 1; x < 79; x++)
            {
                buffer[x, y].Char = (char)196;       //  ─
                buffer[x, y].Attributes = colorGrey;
            }
            buffer[0, y].Char = (char)195;           //  ├
            buffer[0, y].Attributes = colorGrey;
            buffer[79, y].Char = (char)180;          //  ┤
            buffer[79, y].Attributes = colorGrey;
        }

        public static void SingleBoxVerticalDivider(this ScreenBuffer buffer, int x)
        {
            SingleBoxVerticalDivider(buffer,x,0,24);
        }

        public static void Fill(this ScreenBuffer buffer, int x1, int y1, int x2, int y2)
        {
            buffer.Fill(x1, y1, x2, y2, 32 /*32=Space*/, ColorUtility.MakeColor(TextColor.Grey, TextColor.Black));
        }

        public static void SuppressScreenBufferImposters(bool bShouldSuppress = true)
        {
            for (int i = 0; i < 25; i++)
            {
                for (int j = 0; j < 80; j++)
                {
                    ScreenBuffer.ImposterSuppression[j, i] = bShouldSuppress;
                }
            }
        }
        public static void SingleBoxVerticalDivider(this ScreenBuffer buffer, int x, int y1, int y2)
        {
            ushort colorGrey = ColorUtility.MakeColor(TextColor.Grey, TextColor.Black);

            for (int y = y1 + 1; y < y2 ; y++)
            {
                buffer[x, y].Char = (char)179;       //  │
                buffer[x, y].Attributes = colorGrey;
            }
            buffer[x, y1].Char = (char)194;           //  ┬
            buffer[x, y1].Attributes = colorGrey;
            buffer[x, y2].Char = (char)193;          //  ┴
            buffer[x, y2].Attributes = colorGrey;
        }

    }
}
