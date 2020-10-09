using System;

namespace XRL.World.Parts
{
    [Serializable]
    public class QudUX_CommandListener : IPart
    {
        public static readonly string CmdOpenSpriteMenu = "QudUX_OpenSpriteMenu";
        public static readonly string CmdOpenAutogetMenu = "QudUX_OpenAutogetMenu";
        public static readonly string cmdOpenGameStatsMenu = "QudUX_OpenGameStatsMenu";

        public override void Register(GameObject Object)
        {
            Object.RegisterPartEvent(this, CmdOpenSpriteMenu);
            Object.RegisterPartEvent(this, CmdOpenAutogetMenu);
            Object.RegisterPartEvent(this, cmdOpenGameStatsMenu);
            
            base.Register(Object);
        }

        public override bool FireEvent(Event E)
        {
            if (E.ID == CmdOpenSpriteMenu)
            {
                QudUX.Wishes.SpriteMenu.Wish();
            }
            if (E.ID == CmdOpenAutogetMenu)
            {
                QudUX.Wishes.AutopickupMenu.Wish();
            }
            if (E.ID == cmdOpenGameStatsMenu)
            {
                QudUX.Wishes.GameStatsMenu.Wish();
            }
            return base.FireEvent(E);
        }
    }
}
