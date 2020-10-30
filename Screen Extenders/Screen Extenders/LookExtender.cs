using ConsoleLib.Console;
using XRL.UI;
using XRL.World;
using XRL.World.Parts;
using static XRL.World.Parts.QudUX_LegendaryInteractionListener;

namespace QudUX.ScreenExtenders
{
    public class LookExtender
    {
        private static Keys? MarkKey = null;

        public static void AddMarkLegendaryOptionToLooker(ScreenBuffer buffer, GameObject target)
        {
            if ((target.HasProperty("Hero") || target.GetStringProperty("Role") == "Hero") && target.HasPart(typeof(GivesRep)))
            {
                if ((Keys)LegacyKeyMapping.GetKeyFromCommand("CmdWalk") != Keys.M)
                {
                    MarkKey = Keys.M;
                    buffer.Write(" | {{gold|M}} - mark in journal");
                }
                else if ((Keys)LegacyKeyMapping.GetKeyFromCommand("CmdWalk") != Keys.J)
                {
                    MarkKey = Keys.J;
                    buffer.Write(" | {{gold|J}} - mark in journal");
                }
            }
        }

        public static bool CheckKeyPress(Keys key, GameObject target, bool currentKeyFlag)
        {
            if (currentKeyFlag == true) //already processing a different key request
            {
                return true;
            }
            if (MarkKey != null && key == MarkKey && (target.HasProperty("Hero") || target.GetStringProperty("Role") == "Hero") && target.HasPart(typeof(GivesRep)))
            {
                ToggleLegendaryLocationMarker(target);
                return true;
            }
            return false;
        }
    }
}
