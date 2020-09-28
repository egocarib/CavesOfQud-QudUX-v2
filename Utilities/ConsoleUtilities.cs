
using ConsoleLib.Console;

namespace QudUX.Utilities
{
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
            ushort colorGrey = ColorUtility.MakeColor(TextColor.Grey, TextColor.Black);

            for (int y = 1; y < 24; y++)
            {
                buffer[x, y].Char = (char)179;       //  │
                buffer[x, y].Attributes = colorGrey;
            }
            buffer[x, 0].Char = (char)194;           //  ┬
            buffer[x, 0].Attributes = colorGrey;
            buffer[x, 24].Char = (char)193;          //  ┴
            buffer[x, 24].Attributes = colorGrey;
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

    }
}
