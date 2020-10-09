using XRL; // for HasCallAfterGameLoadedAttribute and CallAfterGameLoadedAttribute
using XRL.Core; // for XRLCore
using XRL.World; // for GameObject

namespace XRL.World.Parts
{
  [HasCallAfterGameLoadedAttribute]
  public class QudUXLoadGameHandler
  {
      [CallAfterGameLoadedAttribute]
      public static void GameStats_LoadGameCallback()
      {
          // Called whenever loading a save game
          GameObject player = XRLCore.Core?.Game?.Player?.Body;
          if (player != null)
          {
              player.RequirePart<QudUX_CommandListener>(); //RequirePart will add the part only if the player doesn't already have it. This ensures your part only gets added once, even after multiple save loads.
          }
      }
  }
}
