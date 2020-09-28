using QudUX.Concepts;
using System;

namespace XRL.World.Parts
{
    public class QudUX_ArrowSpriteExchanger : IPart
    {
        public string ReplacementTile;
        public string ReplacementColorString;
        public string ReplacementDetailColor;

        public override bool WantEvent(int ID, int cascade)
        {
            return base.WantEvent(ID, cascade) || ID == ObjectCreatedEvent.ID;
        }

        public override bool HandleEvent(ObjectCreatedEvent E)
        {
            try
            {
                if (Options.Exploration.UseArrowSprite)
                {
                    if (!string.IsNullOrEmpty(ReplacementTile)) //TODO: make conditional on currentTile.ToLower().EndsWith("sw_ammo.bmp")
                    {
                        ParentObject.pRender.Tile = ReplacementTile;
                    }
                    if (!string.IsNullOrEmpty(ReplacementColorString))
                    {
                        ParentObject.pRender.ColorString = ReplacementColorString;
                    }
                    if (!string.IsNullOrEmpty(ReplacementDetailColor))
                    {
                        ParentObject.pRender.DetailColor = ReplacementDetailColor;
                    }
                }
            }
            finally
            {
                ParentObject.RemovePart(this);
            }
            return true;
        }
    }
}
