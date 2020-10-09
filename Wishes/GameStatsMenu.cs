using XRL.UI;
using XRL.Core;
using XRL.Wish;
using System.Text.RegularExpressions;
using QudUX.Utilities;

namespace QudUX.Wishes
{
    [HasWishCommand]
    public class GameStatsMenu
    {
        [WishCommand(Regex = @"(?:[Qq]ud[Uu][Xx] ?)?[Gg]amestats ?[Mm]enu")]
        public static void Wish(Match match = null)
        {
            
            var player = XRLCore.Core?.Game?.Player?.Body;
            if (player != null)
            {
                try
                {
                    ConsoleUtilities.SuppressScreenBufferImposters(true);
                    var GameStatsMenu = new QudUX_GameStatsScreen();
                    GameStatsMenu.Show(player);
                }
                finally
                {
                    ConsoleUtilities.SuppressScreenBufferImposters(false);
                }
            }
        }
    }
}
