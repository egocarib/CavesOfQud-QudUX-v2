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
            try
            {
                var GameStatsMenu = new QudUX_GameStatsScreen();
                GameStatsMenu.Show(null);
            }
            catch (Exception ex)
            {
                Utilities.Logger.Log($"(Error) Encountered an exception while showing the Game Stats menu [{ex}]");
            }
        }
    }
}
