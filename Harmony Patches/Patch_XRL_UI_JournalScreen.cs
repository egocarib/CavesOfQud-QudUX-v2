using System.Collections.Generic;
using HarmonyLib;
using XRL.UI;
using QudUX.ScreenExtenders;
using Options = QudUX.Concepts.Options;

namespace QudUX.HarmonyPatches
{
    [HarmonyPatch(typeof(XRL.UI.JournalScreen))]
    public class Patch_XRL_UI_JournalScreen
    {
        public static readonly string ExploredPrefix = "{{G|\u00fb}}";
        public static readonly string UnexploredPrefix = "{{K|?}}";

        [HarmonyPostfix]
        [HarmonyPatch("UpdateEntries")]
        static void Postfix(string selectedTab, List<JournalScreen.JournalEntry> ___entries, List<string> ___displayLines, List<int> ___entryForDisplayLine)
        {
            if (!Options.Exploration.TrackLocations)
            {
                return; //feature is disabled
            }
            if (selectedTab != JournalScreen.STR_LOCATIONS || ___entries.Count == 0)
            {
                return;
            }
            for (int i = 0; i < ___entries.Count; i++)
            {
                JournalScreen.JournalEntry entry = ___entries[i];
                if (entry.IsALocation())
                {
                    string option = ___displayLines[entry.topLine];
                    int insertPos = 0;
                    if (option.StartsWith("&G$&y ") || option.StartsWith("&K$&y "))
                    {
                        insertPos = 5;
                    }
                    else if (option.StartsWith("{{G|$}} ") || option.StartsWith("{{K|$}} "))
                    {
                        insertPos = 7;
                    }
                    else
                    {
                        QudUX.Utilities.Logger.LogUnique($"(Error) Failed to add visited indicator to journal: Unexpected journal location entry format: \"{option}\"");
                    }
                    if (insertPos > 0)
                    {
                        option = option.Insert(insertPos,
                            entry.HasBeenVisited() ? ExploredPrefix : UnexploredPrefix);
                        ___displayLines[entry.topLine] = option;
                    }
                }
            }
        }
    }
}
