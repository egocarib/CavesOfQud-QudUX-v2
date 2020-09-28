using System;
using XRL.Language;
using XRL.UI;
using QudUX_Constants = QudUX.Concepts.Constants;

namespace XRL.World.Parts
{
    [Serializable]
    public class QudUX_AutogetHelper : IPart
    {
        private static bool TemporarilyIgnoreQudUXSettings;
        private static NameValueBag _AutogetSettings;
        public static NameValueBag AutogetSettings
        {
            get
            {
                if (_AutogetSettings == null)
                {
                    _AutogetSettings = new NameValueBag(QudUX_Constants.AutogetDataFilePath);
                    _AutogetSettings.Load();
                }
                return _AutogetSettings;
            }
        }
        public static readonly string CmdDisableAutoget = "QudUX_DisableItemAutoget";
        public static readonly string CmdEnableAutoget = "QudUX_EnableItemAutoget";

        public static bool IsAutogetDisabledByQudUX(GameObject thing)
        {
            if (TemporarilyIgnoreQudUXSettings)
            {
                return false;
            }
            else if (thing.Understood() == false) //use default behavior if it hasn't been identified yet
            {
                return false;
            }
            return AutogetSettings.GetValue($"ShouldAutoget:{thing.Blueprint}", "").EqualsNoCase("No");
        }

        public override bool WantEvent(int ID, int cascade)
        {
            return base.WantEvent(ID, cascade) || ID == OwnerGetInventoryActionsEvent.ID || ID == InventoryActionEvent.ID;
        }

        public override bool HandleEvent(OwnerGetInventoryActionsEvent E)
        {
            if (!QudUX.Concepts.Options.UI.EnableAutogetExclusions)
            {
                return base.HandleEvent(E);
            }
            bool wasDropped = E.Object.HasIntProperty("DroppedByPlayer");
            if (wasDropped)
            {
                //temporarily remove property so it doesn't affect ShouldAutoget() logic
                E.Object.RemoveIntProperty("DroppedByPlayer");
            }
            TemporarilyIgnoreQudUXSettings = true;
            bool isAutogetItem = E.Object.ShouldAutoget();
            TemporarilyIgnoreQudUXSettings = false;
            if (wasDropped)
            {
                E.Object.SetIntProperty("DroppedByPlayer", 1);
            }
            if (isAutogetItem && E.Object.Understood())
            {
                if (IsAutogetDisabledByQudUX(E.Object))
                {
                    E.AddAction("Re-enable auto-pickup for this item", "re-enable auto-pickup", CmdEnableAutoget, FireOnActor: true);
                }
                else
                {
                    E.AddAction("Disable auto-pickup for this item", "disable auto-pickup", CmdDisableAutoget, FireOnActor: true);
                }
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(InventoryActionEvent E)
        {
            if (E.Command == CmdDisableAutoget)
            {
                bool bInfoboxShown = AutogetSettings.GetValue("Metadata:InfoboxWasShown", "").EqualsNoCase("Yes");
                if (!bInfoboxShown)
                {
                    DialogResult choice = DialogResult.Cancel;
                    while (choice != DialogResult.Yes && choice != DialogResult.No)
                    {
                        choice = Popup.ShowYesNo("Disabling auto-pickup for " + Grammar.Pluralize(E.Item.DisplayNameOnly) + ".\n\n"
                            + "Changes to auto-pickup preferences will apply to ALL of your characters. "
                            + "If you proceed, this message will not be shown again.\n\nProceed?", false, DialogResult.Cancel);
                    }
                    if (choice == DialogResult.Yes)
                    {
                        AutogetSettings.SetValue("Metadata:InfoboxWasShown", "Yes", FlushToFile: false);
                        AutogetSettings.SetValue($"ShouldAutoget:{E.Item.Blueprint}", "No");
                    }
                }
                else
                {
                    AutogetSettings.SetValue($"ShouldAutoget:{E.Item.Blueprint}", "No");
                }
            }
            if (E.Command == CmdEnableAutoget)
            {
                AutogetSettings.Bag.Remove($"ShouldAutoget:{E.Item.Blueprint}");
                AutogetSettings.Flush();
            }
            return base.HandleEvent(E);
        }
    }
}