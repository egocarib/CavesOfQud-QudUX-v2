using System;
using System.Text.RegularExpressions;
using XRL.UI;
using XRL.Wish;
using XRL.World;
using QudUX.Utilities;

namespace QudUX.Wishes
{
    [HasWishCommand]
    public class FlipTile
    {
        [WishCommand(Regex = @"[Ff]lip(?: ?[Tt]ile)?")]
        public static void Wish(Match match = null)
        {
            Utilities.Logger.Log("Wish invoked: Fliptile");
            var player = IComponent<GameObject>.ThePlayer;
            if (player != null && player.pPhysics != null)
            {
                try
                {
                    Cell cell = player.pPhysics.PickDestinationCell(
                        Range: 999,
                        Vis: AllowVis.OnlyExplored,
                        Locked: false,
                        Style: PickTarget.PickStyle.EmptyCell);
                    if (cell != null)
                    {
                        GameObject target = cell.GetHighestRenderLayerObject();
                        if (target == null)
                        {
                            Popup.Show("There's no object in that spot.");
                            return;
                        }
                        TextureMaker.FlipGameObjectTexture(target);
                    }
                }
                catch (Exception ex)
                {
                    QudUX.Utilities.Logger.Log($"(Error) Failed to process Fliptile wish.\nException Details:\n{ex.ToString()}");
                }
            }
        }
    }
}
