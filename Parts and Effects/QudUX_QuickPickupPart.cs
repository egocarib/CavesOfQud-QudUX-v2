using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleLib.Console;
using HarmonyLib;
using QudUX.AutoActActions;
using QudUX.Concepts;
using XRL.Core;
using XRL.UI;
using XRL.World.Capabilities;

namespace XRL.World.Parts
{
    public class QudUX_QuickPickupPart : IPart
    {
        private const string _DisplaySettings = "QudUX_OpenQuickPickupSettingsMenu";
        private const string _DisplayQuickMenu = "QudUX_OpenQuickPickupQuickMenu";
        public int LastSelectedTab = 0;

        // Look for int property
        public Dictionary<string, ValueTuple<bool, string>> TierSettings = new Dictionary<string, ValueTuple<bool, string>>
        {
            { "Tier 0", (true, "0") },
            { "Tier 1", (true, "1") },
            { "Tier 2", (true, "2") },
            { "Tier 3", (true, "3") },
            { "Tier 4", (true, "4") },
            { "Tier 5", (true, "5") },
            { "Tier 6", (true, "6") },
            { "Tier 7", (true, "7") },
            { "Tier 8", (true, "8") },
        };

        // Look for inheritance of Tuple.Item2
        public Dictionary<string, ValueTuple<bool, string>> TypesSettings = new Dictionary<string, ValueTuple<bool, string>>
        {
            { "Tools",              (true, "Tool") },
            { "Axes",               (true, "BaseAxe") },
            { "Daggers",            (true, "BaseDagger") },
            { "Long Blades",        (true, "BaseLongBlade") },
            { "Cudgels",            (true, "BaseCudgel") },
            { "Shields",            (true, "BaseShield") },
            { "Heavy Weapons",      (true, "BaseHeavyWeapon") },
            { "Bows",               (true, "BaseBow") },
            { "Pistols",            (true, "BasePistol") },
            { "Rifles",             (true, "BaseRifle") },
        };

        // Look for part "Armor" worn on Tuple.item2
        public Dictionary<string, ValueTuple<bool, string>> ArmorSettings = new Dictionary<string, ValueTuple<bool, string>>
        {
            { "Head",               (true, "Head")},
            { "Face",               (true, "Face")},
            { "Body",               (true, "Body")},
            { "Hand",               (true, "Hands")},
            { "Arm",                (true, "Arm")},
            { "Feet",               (true, "Feet")},
            { "Back",               (true, "Back")},
            { "Floating Nearby",    (true, "Floating Nearby")},
        };

        public override void Register(GameObject Object)
        {
            Object.RegisterPartEvent(this, _DisplayQuickMenu);
            Object.RegisterPartEvent(this, _DisplaySettings);

            base.Register(Object);
        }

        public override bool FireEvent(Event E)
        {
            if (E.ID == _DisplayQuickMenu)
            {
                BuildPopup();
            }

            if (E.ID == _DisplaySettings)
            {
                var screen = new QudUX_QuickPickupSettingsScreen();
                screen.Show(ParentObject);
            }

            return base.FireEvent(E);
        }

        private void BuildPopup()
        {
            List<GameObject> selection = null;
            foreach (Cell currentCell in ParentObject.CurrentZone.GetExploredCells())
            {
                List<GameObject> candidates = new List<GameObject>();
                candidates.AddRange(currentCell.GetObjectsThatInheritFrom("Armor"));
                candidates.AddRange(currentCell.GetObjectsThatInheritFrom("MeleeWeapon"));
                candidates.AddRange(currentCell.GetObjectsThatInheritFrom("MissileWeapon"));
                candidates.AddRange(currentCell.GetObjectsThatInheritFrom("Shield"));
                candidates.AddRange(currentCell.GetObjectsThatInheritFrom("Tool"));

                FilterObjectsOnCell(
                    TypesSettings,
                    candidates,
                    // Filtering on candidate inheritance
                    (GameObject candidate, string value) =>
                        candidate.GetBlueprint().InheritsFrom(value)
                );

                FilterObjectsOnCell(
                    ArmorSettings,
                    candidates,
                    // Filtering armor based on their targeted body part
                    (GameObject candidate, string value) =>
                    {
                        if (candidate.TryGetPart(out Armor armor))
                        {
                            return armor.WornOn == value;
                        }
                        return false;
                    }
                );

                FilterObjectsOnCell(
                    TierSettings,
                    candidates,
                    // Filtering on Tier tag value
                    (GameObject candidate, string value) =>
                        candidate.HasTag("Tier") && candidate.GetTier() == int.Parse(value)
                );

                if (candidates.Count > 0)
                {
                    if (selection == null)
                        selection = new List<GameObject>();

                    selection.AddRange(candidates);
                }

            }

            if (selection == null)
            {
                Popup.Show("There is nothing of interest to pick up here...");
                return;
            }

            List<GameObject> orderedGameObjects = new List<GameObject>();
            foreach(GameObject obj in SelectAndOrderObjects(selection, "MeleeWeapon"))
            {
                orderedGameObjects.Add(obj);
                selection.Remove(obj);
            }

            foreach(GameObject obj in SelectAndOrderObjects(selection, "MissileWeapon"))
            {
                orderedGameObjects.Add(obj);
                selection.Remove(obj);
            }

            foreach(GameObject obj in SelectAndOrderObjects(selection, "Armor"))
            {
                orderedGameObjects.Add(obj);
                selection.Remove(obj);
            }

            selection = orderedGameObjects;

            List<string> options = new List<string>();
            foreach (var obj in selection)
                options.Add(obj.BaseDisplayName);

            List<IRenderable> icons = new List<IRenderable>();
            foreach (var obj in selection)
                icons.Add(obj.RenderForUI());

            var results = Popup.PickSeveral(
                Title: "Which item do you want to get ?",
                Options: options,
                Icons: icons,
                AllowEscape: true
            );

            if (results == null || results.Count == 0) return;

            var selectedItems = new List<GameObject>();

            foreach (var result in results)
                selectedItems.Add(selection[result.Item1]);

            AutoAct.Action = new PickupSelection(selectedItems, ParentObject);
            ParentObject.ForfeitTurn(true);
        }

        public override void LoadData(SerializationReader Reader)
        {
            base.LoadData(Reader);

            Dictionary<string, ValueTuple<bool, string>> newSettings = new Dictionary<string, ValueTuple<bool, string>>();
            foreach (var key in TierSettings.Keys)
            {
                newSettings.Add(
                    key,
                    (Reader.ReadBoolean(), TierSettings[key].Item2)
                );
            }
            TierSettings = newSettings;

            newSettings = new Dictionary<string, ValueTuple<bool, string>>();
            foreach (var key in TypesSettings.Keys)
            {
                newSettings.Add(
                    key,
                    (Reader.ReadBoolean(), TypesSettings[key].Item2)
                );
            }
            TypesSettings = newSettings;

            newSettings = new Dictionary<string, ValueTuple<bool, string>>();
            foreach (var key in ArmorSettings.Keys)
            {
                newSettings.Add(
                    key,
                    (Reader.ReadBoolean(), ArmorSettings[key].Item2)
                );
            }
            ArmorSettings = newSettings;
        }

        public override void SaveData(SerializationWriter Writer)
        {
            base.SaveData(Writer);

            foreach (var key in TierSettings.Keys)
            {
                bool value = TierSettings[key].Item1;
                Writer.Write(value);
            }

            foreach (var key in TypesSettings.Keys)
            {
                bool value = TypesSettings[key].Item1;
                Writer.Write(value);
            }

            foreach (var key in ArmorSettings.Keys)
            {
                bool value = ArmorSettings[key].Item1;
                Writer.Write(value);
            }
        }

        private void FilterObjectsOnCell(Dictionary<string, ValueTuple<bool, string>> settings, List<GameObject> candidates, Func<GameObject, string, bool> predicate)
        {
            List<GameObject> filteredObjects = new List<GameObject>();
            foreach (var key in settings.Keys)
            {
                //Step out if we don't have any other object
                if (candidates.Count == 0) return;

                var tuple = settings[key];

                // Filtering candidates using passed on predicate, fed with tuple value
                filteredObjects = candidates.Where(obj => predicate(obj, tuple.Item2)).ToList();

                // if filter is set to disabled, remove found objects from candidates
                if (!tuple.Item1)
                {
                    foreach (GameObject obj in filteredObjects)
                        candidates.Remove(obj);
                }
            }
        }
    
        private IEnumerable<GameObject> SelectAndOrderObjects(List<GameObject> objects, string parentBlueprint)
        {
            return objects.Where(obj => obj.GetBlueprint().InheritsFrom(parentBlueprint))
                            .OrderByDescending(obj => {
                                if(!obj.HasTag("Tier")) return 0;
                                return obj.GetTier();
                            });
        }
    }
}