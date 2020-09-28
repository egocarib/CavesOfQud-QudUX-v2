using XRL.Core;
using XRL.World;
using XRL.World.Parts;
using QudUX.ScreenExtenders;

namespace QudUX.Concepts
{
    //Custom events that are called from Patch_XRL_Core_XRLCore.
    //The player object (XRLCore.Core.Game.Player.Body) is available for use in all of these events.
    public static class Events
    {
        private static GameObject Player => XRLCore.Core?.Game?.Player?.Body;

        //Event fired on new character just before embarking. This event occurs later than PlayerMutator
        //which allows doing certain things that would otherwise be impossible, like setting the player's
        //Tile. Normally player tile is overwritten by XRLCore after PlayerMutator runs.
        public static void EmbarkEvent()
        {
            CreateCharacterExtender.ApplyTileInfoDeferred();
        }

        //Runs immediately after a save is loaded.
        public static void SaveLoadEvent()
        {

        }

        //Runs in all load scenarios - called immediately after each of the events above.
        public static void OnLoadAlwaysEvent()
        {
            if (Player != null)
            {
                Player.RequirePart<QudUX_AutogetHelper>();
                Player.RequirePart<QudUX_CommandListener>();
                Player.RequirePart<QudUX_ConversationHelper>();
                Player.RequirePart<QudUX_LegendaryInteractionListener>();
            }
        }
    }
}
