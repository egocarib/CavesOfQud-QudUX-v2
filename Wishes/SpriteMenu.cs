using XRL.UI;
using XRL.Core;
using XRL.Wish;
using QudUX.Utilities;
using System.Text.RegularExpressions;

namespace QudUX.Wishes
{
    [HasWishCommand]
    public class SpriteMenu
    {
        [WishCommand(Regex = @"(?:[Qq]ud[Uu][Xx] ?)?[Ss]prite ?[Mm]enu")]
        public static void Wish(Match match = null)
        {
            Utilities.Logger.Log("Wish invoked: Open Sprite Menu");
            var player = XRLCore.Core?.Game?.Player?.Body;
            if (player != null)
            {
                try
                {
                    ConsoleUtilities.SuppressScreenBufferImposters(true);
                    var SpriteMenu = new QudUX_CharacterTileScreen();
                    SpriteMenu.Show(player);
                }
                finally
                {
                    ConsoleUtilities.SuppressScreenBufferImposters(false);
                }
            }
        }
    }
}
