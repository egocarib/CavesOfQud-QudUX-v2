using System;
using System.IO;
using XRL.Core;
using XRL.Rules;
using XRL.Names;
using static QudUX.Utilities.Logger;

namespace QudUX.Utilities
{
    public class JournalUtilities
    {
        private readonly static string[] NamePrefixes = new string[]
        {
            "U", "Ma", "Ka", "Mi", "Shu", "Ha", "Ala", "A", "Da", "Bi", "Ta", "Te", "Tu",
            "Sa", "Du", "Na", "She", "Sha", "Eka", "Ki", "I", "Su", "Qa"
        };
        private readonly static string[] NameInfixes = new string[]
        {
            "rche", "ga", "rva", "mri", "azo", "arra", "ili", "ba", "gga", "rqa", "rqu",
            "by", "rsi", "ra", "ne"
        };
        private readonly static string[] NamePostfixes = new string[]
        {
            "ppur", "ppar", "ppir", "sh", "d", "mish", "kh", "mur", "bal", "mas", "zor",
            "mor", "nip", "lep", "pad", "kesh", "war", "tum", "mmu", "mrod", "shur",
            "nna", "kish", "ruk", "r", "ppa", "wan", "shan", "tara", "vah", "vuh", "lil"
        };

        /// <summary>
        /// Custom name generator. Very similar to QudHistoryFactory.NameRuinsSite but uses some custom prefixes
        /// and postfixes I made up for variety. The base game's NameRuinsSite actually doesn't give a lot of
        /// variety, using only that and randomness I have seen >100 duplicates in a set of ~550 named locations.
        /// </summary>
        public static string GenerateName()
        {
            string text;
            text = NameMaker.MakeName(null, null, null, null, "Qudish", null, null, null, "Site");

            int chance = 50;
            if (chance.in100())
            {
                int num = Stat.Random(0, 22);
                switch (num)
                {
                    case 0:
                        text += " Schism"; break;
                    case 1:
                        text = "the Sliver of " + text; break;
                    case 2:
                        text = "the Glyph at " + text; break;
                    case 3:
                        text += " Roof"; break;
                    case 4:
                        text += " Fist"; break;
                    case 5:
                        text = "Ethereal " + text; break;
                    case 6:
                        text = "Aged " + text; break;
                    case 7:
                        text += " Sluice"; break;
                    case 8:
                        text += " Aperture"; break;
                    case 9:
                        text += " Disc"; break;
                    case 10:
                        text += " Clearing"; break;
                    case 11:
                        text += " Pass"; break;
                    case 12:
                        text += " Seam"; break;
                    case 13:
                        text += " Twinge"; break;
                    case 14:
                        text += " Grist"; break;
                    case 15:
                        text = "Dilapidated " + text; break;
                    case 16:
                        text = "Forgotten " + text; break;
                    case 17:
                        text = "Crumbling " + text; break;
                    case 18:
                        text = "Tangential " + text; break;
                    case 19:
                        text += " Vents"; break;
                    case 20:
                        text += " Lookabout"; break;
                    case 21:
                        text += " Furrow"; break;
                    case 22:
                        text = "Distasteful " + text; break;
                }
            }
            return text;
        }

        public static bool FrozenZoneDataExists(string zoneID) //Unfortunately need this function because ZoneManager.FrozenZones is private
        {
            try
            {
                string compressedZoneFile = Path.Combine(Path.Combine(XRLCore.Core.Game.GetCacheDirectory(), "ZoneCache"), zoneID + ".zone.gz");
                if (File.Exists(compressedZoneFile))
                {
                    return true;
                }
                string pureZoneFile = Path.Combine(Path.Combine(XRLCore.Core.Game.GetCacheDirectory(), "ZoneCache"), zoneID + ".zone");
                if (File.Exists(pureZoneFile))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log($"Encountered exception while checking for frozen zone: {ex}");
            }
            return false;
        }
    }
}
