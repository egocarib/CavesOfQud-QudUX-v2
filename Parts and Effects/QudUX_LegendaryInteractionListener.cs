using System;
using System.Collections.Generic;
using Qud.API;
using XRL.UI;

namespace XRL.World.Parts
{
    [Serializable]
    class QudUX_LegendaryInteractionListener : IPart
    {
        public static readonly string CmdJournalMarkLegendary = "CmdJournalMarkLegendary";
        public static readonly string CmdJournalUnmarkLegendary = "CmdJournalUnmarkLegendary";

        public override bool WantEvent(int ID, int cascade)
        {
            return base.WantEvent(ID, cascade) || ID == OwnerGetInventoryActionsEvent.ID || ID == InventoryActionEvent.ID;
        }

        public override bool HandleEvent(OwnerGetInventoryActionsEvent E)
        {
            if ((E.Object.HasProperty("Hero") || E.Object.GetStringProperty("Role") == "Hero") && E.Object.HasPart(typeof(GivesRep)))
            {
                if (JournalAPI.GetMapNote(MakeSecretId(E.Object)) == null)
                {
                    E.AddAction("Mark Legendary Location in Journal", "mark location in journal", CmdJournalMarkLegendary, FireOnActor: true, WorksAtDistance: true, WorksTelepathically: true);
                }
                else
                {
                    E.AddAction("Remove Marked Legendary Location from Journal", "unmark location in journal", CmdJournalUnmarkLegendary, FireOnActor: true, WorksAtDistance: true, WorksTelepathically: true);
                }
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(InventoryActionEvent E)
        {
            if (E.Command == CmdJournalMarkLegendary && E.Item != null)
            {
                MarkLegendaryLocation(E.Item);
            }
            else if (E.Command == CmdJournalUnmarkLegendary && E.Item != null)
            {
                UnmarkLegendaryLocation(E.Item);
            }
            return base.HandleEvent(E);
        }

        public static void MarkLegendaryLocation(GameObject target)
        {
            string entryText = $"{target.DisplayNameOnlyDirect}{ParentheticalListOfRelations(target)}";
            string secret = MakeSecretId(target);
            JournalAPI.AddMapNote(target.CurrentZone.ZoneID, entryText, "Legendary Creatures", secretId: secret, revealed: true, sold: true, silent: true);
            string text = "You note the location of " + target.DisplayNameOnlyDirect + "&y in the &W" + JournalScreen.STR_LOCATIONS + " > Legendary Creatures &ysection of your journal.";
            Popup.Show(text);
        }

        public static void UnmarkLegendaryLocation(GameObject target)
        {
            JournalMapNote mapNote = JournalAPI.GetMapNote(MakeSecretId(target));
            if (mapNote != null)
            {
                JournalAPI.DeleteMapNote(mapNote);
                Popup.Show("Your journal entry for " + target.DisplayNameOnlyDirect + "&y has been deleted.");
            }
        }

        public static void ToggleLegendaryLocationMarker(GameObject target)
        {
            if (JournalAPI.GetMapNote(MakeSecretId(target)) == null)
            {
                MarkLegendaryLocation(target);
            }
            else
            {
                UnmarkLegendaryLocation(target);
            }
        }

        public static string MakeSecretId(GameObject legendaryCreature)
        {
            return $"QudUX_{legendaryCreature.DisplayNameOnlyDirectAndStripped}_{legendaryCreature.CurrentZone.ZoneID}";
        }

        public static string ParentheticalListOfRelations(GameObject legendaryCreature)
        {
            GivesRep repInfo = legendaryCreature.GetPart<GivesRep>();
            if (repInfo == null)
            {
                return string.Empty;
            }
            List<string> relationList = new List<string>();
            foreach (string key in legendaryCreature.pBrain.FactionMembership.Keys)
            {
                Faction ifExists = Factions.getIfExists(key);
                if (ifExists != null && ifExists.Visible)
                {
                    relationList.Add("Loved by {{C|" + ifExists.getFormattedName() + "}}");
                }
            }
            foreach (FriendorFoe relatedFaction in repInfo.relatedFactions)
            {
                Faction ifExists2 = Factions.getIfExists(relatedFaction.faction);
                if (ifExists2 != null && ifExists2.Visible)
                {
                    if (relatedFaction.status == "friend")
                    {
                        relationList.Add("Admired by {{C|" + Faction.getFormattedName(relatedFaction.faction) + "}}");
                    }
                    else if (relatedFaction.status == "dislike")
                    {
                        relationList.Add("Disliked by {{C|" + Faction.getFormattedName(relatedFaction.faction) + "}}");
                    }
                    else if (relatedFaction.status == "hate")
                    {
                        relationList.Add("Hated by {{C|" + Faction.getFormattedName(relatedFaction.faction) + "}}");
                    }
                }
            }
            string relations = string.Empty;
            for (int i = 0; i < relationList.Count; i++)
            {
                if (i == 0)
                {
                    relations += "{{y| (";
                }
                else if (i > 0 && i < relationList.Count)
                {
                    relations += ", ";
                }
                relations += relationList[i];
            }
            if (relationList.Count > 0)
            {
                relations += ")}}";
            }
            return relations;
        }
    }
}
