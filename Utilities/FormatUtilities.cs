using ConsoleLib.Console;
using System.Security.Cryptography;

namespace QudUX.Utilities
{
    public static class FormatUtilities
    {
        /// <summary>
        /// returns a string of (non-breaking) spaces that can be used to format text. Particularly
        /// useful in Popup.Show, which otherwise strips out any sequence of multiple spaces.
        /// </summary>
        public static string Spaces(int numberOfSpaces)
        {
            string ret = "{{k|";
            while (numberOfSpaces-- > 0)
            {
                ret += "\u00ff";
            }
            ret += "}}";
            return ret;
        }

        public static string PadRight(string formattedString, int desiredWidth)
        {
            int currentWidth = ColorUtility.LengthExceptFormatting(formattedString);
            if (currentWidth < desiredWidth)
            {
                formattedString += Spaces(desiredWidth - currentWidth);
            }
            return formattedString;
        }
    }
}
