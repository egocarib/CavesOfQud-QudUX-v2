using XRL.UI;
using XRL.Wish;
using QudUX.Utilities;
using System.Text.RegularExpressions;

namespace QudUX.Wishes
{
    [HasWishCommand]
    public class AutopickupMenu
    {
        [WishCommand(Regex = @"(?:[Qq]ud[Uu][Xx] ?)?[Aa]uto-?[Pp]ickup ?[Mm]enu")]
        public static void Wish(Match match = null)
        {
            Utilities.Logger.Log("Wish invoked: Open Auto-pickup Menu");
            if (!Concepts.Options.UI.EnableAutogetExclusions)
            {
                Popup.Show("You've disabled the option to {{C|Use item interaction menu to disable auto-pickup for specific items}}."
                    + " You must re-enable this option if you want to use the QudUX Auto-pickup menu.");
            }
            else
            {
                try
                {
                    ConsoleUtilities.SuppressScreenBufferImposters(true);
                    var AutogetMenu = new QudUX_AutogetManagementScreen();
                    AutogetMenu.Show(null);
                }
                finally
                {
                    ConsoleUtilities.SuppressScreenBufferImposters(false);
                }
            }
        }
    }
}
