using System;
using XRL.Core;
using XRL.World.Parts;

namespace XRL.World.Effects
{
    [Serializable]
    public class QudUX_QuestGiverVision : Effect
    {
        public readonly long StartTurn;
        public readonly long CustomDuration = 250;
        public GameObject Seeker;
        public readonly string questGiverIcon = "*";
        public readonly string questGiverIconColor = "&G";

        public QudUX_QuestGiverVision()
        {
            this.Duration = 1;
            this.StartTurn = XRLCore.Core.Game.Turns;
        }

        public QudUX_QuestGiverVision(GameObject Seeker) : this()
        {
            this.Seeker = Seeker;
        }

        public override bool SameAs(Effect e)
        {
            return false;
        }

        public override string GetDescription()
        {
            return null;
        }

        private bool BadListener()
        {
            this.Seeker = null;
            this.Object.RemoveEffect(this);
            return true;
        }

        public bool CheckListen()
        {
            if (this.Seeker == null || this.Seeker.IsInvalid() || !this.Seeker.IsPlayer())
            {
                return this.BadListener();
            }
            if ((XRLCore.Core.Game.Turns - this.StartTurn) > this.CustomDuration)
            {
                return this.BadListener();
            }
            return true;
        }

        public override bool Apply(GameObject Object)
        {
            Brain pBrain = Object.pBrain;
            if (pBrain != null)
            {
                pBrain.Hibernating = false;
            }
            this.CheckListen();
            return true;
        }

        public override void Register(GameObject Object)
        {
            Object.RegisterEffectEvent(this, "EndTurn");
            Object.RegisterEffectEvent(this, "ZoneActivated");
            Object.RegisterEffectEvent(this, "BeginConversation");
            base.Register(Object);
        }

        public override void Unregister(GameObject Object)
        {
            Object.UnregisterEffectEvent(this, "EndTurn");
            Object.UnregisterEffectEvent(this, "ZoneActivated");
            Object.UnregisterEffectEvent(this, "BeginConversation");
            base.Unregister(Object);
        }

        public bool KnownButNotSeen(GameObject obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (!obj.IsVisible())
            {
                return true;
            }
            Cell currentCell = obj.CurrentCell;
            return currentCell != null && (!currentCell.IsLit() || !currentCell.IsExplored());
        }

        public override bool FinalRender(RenderEvent E, bool bAlt)
        {
            if (this.KnownButNotSeen(this.Object) && this.Object.FireEvent("CanHypersensesDetect"))
            {
                int frame200 = XRLCore.CurrentFrame10 % 200;
                int midDistance = Math.Max((Math.Abs(100 - frame200) - 30), 0) / 10;
                if (frame200 % 5 < (5 - midDistance))
                {
                    E.Tile = this.Object.pRender.Tile;
                    E.ColorString = "&K";
                }
                else
                {
                    E.Tile = null;
                    E.RenderString = this.questGiverIcon;
                    E.ColorString = this.questGiverIconColor;
                }
                E.CustomDraw = true;
                return false;
            }
            else if (this.Object != null)
            {
                int frame200 = XRLCore.CurrentFrame10 % 200;
                int midDistance = Math.Max((Math.Abs(100 - frame200) - 40), 0) / 10;
                if (frame200 % 5 >= (5 - midDistance))
                {
                    E.Tile = null;
                    E.RenderString = this.questGiverIcon;
                    E.ColorString = this.questGiverIconColor;
                    E.CustomDraw = true;
                }
                return false;
            }
            return true;
        }

        public override bool FireEvent(Event E)
        {
            if (E.ID == "BeginConversation")
            {
                if (E.GetGameObjectParameter("With") == XRLCore.Core.Game.Player.Body)
                {
                    this.BadListener(); //remove visual effect when player starts a conversation with this NPC
                }
            }
            else if ((E.ID == "EndTurn" || E.ID == "ZoneActivated") && this.Object != null)
            {
                this.CheckListen();
            }
            return base.FireEvent(E);
        }

    }
}
