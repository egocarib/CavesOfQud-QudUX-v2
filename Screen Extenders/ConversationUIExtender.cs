using ConsoleLib.Console;
using XRL.Core;
using XRL.World;
using QudUX.Utilities;
using Options = QudUX.Concepts.Options;

namespace QudUX.ScreenExtenders
{
    public static class ConversationUIExtender
    {
        /// <summary>
        /// This function is called from our Harmony patch code after the Conversation UI title is drawn.
        /// It inserts the speaker's tile into the title and disables any Unity prefabs currently being
        /// animated under that tile.
        /// </summary>
        /// <remarks>
        /// We could probably cache the speakerTileInfo instead of recreating it every time the screen is
        /// redrawn, but this works fine as-is and there's no noticeable performance issues.
        /// </remarks>
        public static void DrawConversationSpeakerTile(ScreenBuffer screenBuffer, GameObject speaker)
        {
            if (!Options.UI.AddConversationTiles || XRLCore.Core.ConfusionLevel > 0)
            {
                return;
            }
            GameObject player = XRLCore.Core?.Game?.Player?.Body;
            if (player == null)
            {
                return; //theoretically should never happen
            }
            screenBuffer.X -= 1; //backspace to where the ']' was drawn
            TileMaker speakerTileInfo = new TileMaker(speaker);
            speakerTileInfo.WriteTileToBuffer(screenBuffer);
            screenBuffer.Write("{{y| ]}}");
        }
    }
}
