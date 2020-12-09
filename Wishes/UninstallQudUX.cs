using System;
using System.Text.RegularExpressions;
using XRL.UI;
using XRL.Core;
using XRL.Wish;
using XRL.World;
using XRL.World.Parts;
using XRL.World.Effects;
using QudUX.Utilities;

namespace QudUX.Wishes
{
    [HasWishCommand]
    public class UninstallQudUX
    {
        [WishCommand(Regex = @"[Uu]ninstall ?[Qq]ud[Uu][Xx]")]
        public static void Wish(Match match = null)
        {
            Utilities.Logger.Log("Attempting to remove QudUX object parts to prepare for safe uninstall...");
            var player = XRLCore.Core?.Game?.Player?.Body;
            if (player != null)
            {
                try
                {
                    Zone parentZone = player.CurrentCell.ParentZone;
                    for (int i = 0; i < parentZone.Width; i++)
                    {
                        for (int j = 0; j < parentZone.Height; j++)
                        {
                            foreach (GameObject thing in parentZone.GetCell(i, j).GetObjectsWithPart("Physics"))
                            {
                                if (thing.HasPart(typeof(QudUX_AutogetHelper)))
                                {
                                    thing.RemovePart<QudUX_AutogetHelper>();
                                }
                                if (thing.HasPart(typeof(QudUX_CommandListener)))
                                {
                                    thing.RemovePart<QudUX_CommandListener>();
                                }
                                if (thing.HasPart(typeof(QudUX_ConversationHelper)))
                                {
                                    thing.RemovePart<QudUX_ConversationHelper>();
                                }
                                if (thing.HasPart(typeof(QudUX_InventoryScreenState)))
                                {
                                    thing.RemovePart<QudUX_InventoryScreenState>();
                                }
                                if (thing.HasPart(typeof(QudUX_LegendaryInteractionListener)))
                                {
                                    thing.RemovePart<QudUX_LegendaryInteractionListener>();
                                }
                                if (thing.HasEffect<QudUX_QuestGiverVision>())
                                {
                                    thing.RemoveEffect(typeof(QudUX_QuestGiverVision));
                                }
                                TextureMaker.UnflipGameObjectTexture(thing);
                            }
                        }
                    }
                    Utilities.Logger.Log("Finished removing QudUX parts from all creatures in the current zone.");
                    Popup.Show("All QudUX parts and effects have been removed from creatures in the current zone.\n\n"
                        + "It's now safe to save your game, close Caves of Qud, and uninstall QudUX.\n\n"
                        + "If all worked right, you should be able to reload this save without QudUX installed.", LogMessage: false);
                }
                catch (Exception ex)
                {
                    Utilities.Logger.Log($"Encountered an exception while trying to remove QudUX parts from creatures in the current zone. [{ex}]");
                    Popup.Show($"There was an error trying to remove QudUX parts and effects from creatures in the current zone.\n\n{ex}", LogMessage: false);
                }
			}
        }
    }
}
