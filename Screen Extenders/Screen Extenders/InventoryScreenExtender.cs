using System;
using System.Collections.Generic;
using XRL.Core;
using XRL.World;
using XRL.World.Parts;
using QudUX.Concepts;
using QudUX.Utilities;
using static QudUX.Utilities.Logger;

namespace QudUX.ScreenExtenders
{
    // Created circa beta patch version 2.0.201.20
    public static class InventoryScreenExtender
    {
        public static class HelpText
        {
            private static readonly Func<string, string, string> JoinLine = (s1, s2) =>
            {
                return FormatUtilities.Spaces(2) + FormatUtilities.PadRight(s1, 27) + s2 + "\n";
            };

            private static readonly string sControlsBase =
                "{{Y|Inventory controls}}\n"
                + JoinLine("Scroll", "{{W|2}}/{{W|8}} or {{W|PgDn}}/{{W|PgUp}}")
                + JoinLine("Switch tab", "{{W|4}} or {{W|6}}")
                + JoinLine("Twiddle item", "{{W|Space}} or {{W|Enter}}")
                + JoinLine("Collapse/expand all", "{{W|+}} or {{W|-}}")
                + JoinLine("Collapse/expand current", "{{W|1}} or {{W|3}}");

            private static readonly string sControlsValueMode =
                JoinLine("Toggle weight/value mode", "{{W|.}} or {{W|0}}");

            private static readonly string sControlsEnd =
                JoinLine("Filter", "{{W|,}} or {{W|Ctrl}}+{{W|F}}");

            private static readonly string sQuickActions =
                "\n{{Y|Item quick actions}}\n"
                + JoinLine("Look", "{{W|Tab}}")
                + JoinLine("Drop", "{{W|Ctrl}}+{{W|D}}")
                + JoinLine("Auto-equip", "{{W|Ctrl}}+{{W|E}}")
                + JoinLine("Eat", "{{W|Ctrl}}+{{W|A}}")
                + JoinLine("Drink", "{{W|Ctrl}}+{{W|R}}")
                + JoinLine("Apply", "{{W|Ctrl}}+{{W|P}}");

            public static string FormattedString
            {
                get
                {
                    if (Options.UI.ViewItemValues)
                    {
                        return "{{y|" + sControlsBase + sControlsValueMode + sControlsEnd + sQuickActions + "}}";
                    }
                    return "{{y|" + sControlsBase + sControlsEnd + sQuickActions + "}}";
                }
            }

            public static void Show()
            {
                XRL.UI.Popup.Show(FormattedString, LogMessage: false);
            }
        }

        public class TabController
        {
            private readonly GameObject Holder;
            private int TabIndex = 0;
            public static List<string> Tabs = new List<string>()
            {
                "Main",
                "Liquids",
                "Tonics",
                "Food",
                "Books",
                "Trade Goods",
                "Other"
            };
            private static readonly Dictionary<string, List<string>> TabCategories = new Dictionary<string, List<string>>
            {
                { "Main",        null },
                { "Liquids",     new List<string> { "Water Container" } },
                { "Tonics",      new List<string> { "Tonics", "Meds" } },
                { "Food",        new List<string> { "Food" } },
                { "Books",       new List<string> { "Books" } },
                { "Trade Goods", new List<string> { "Trade Goods" } },
                { "Other",       new List<string> { } },
            };
            private static readonly Dictionary<string, string> CategoryToTab = new Dictionary<string, string>();

            public TabController(GameObject holder)
            {
                Holder = holder;
                var savedState = Holder.RequirePart<QudUX_InventoryScreenState>();
                TabCategories["Other"].Clear();
                foreach (string category in savedState.CategoriesInOtherTab)
                {
                    TabCategories["Other"].Add(category);
                }
                UpdateCategoryMappings();
            }

            public List<string> GetCategoriesForTab(string tabName)
            {
                return TabCategories.ContainsKey(tabName) ? TabCategories[tabName] : null;
            }

            public string CurrentTab => Tabs[TabIndex];

            public void Forward()
            {
                if (++TabIndex >= Tabs.Count)
                {
                    TabIndex = 0;
                }
            }

            public void Back()
            {
                if (--TabIndex < 0)
                {
                    TabIndex = Tabs.Count - 1;
                }
            }

            private void UpdateCategoryMappings()
            {
                CategoryToTab.Clear();
                foreach (KeyValuePair<string, List<string>> tabInfo in TabCategories)
                {
                    if (tabInfo.Value != null)
                    {
                        foreach (string category in tabInfo.Value)
                        {
                            CategoryToTab[category] = tabInfo.Key;
                        }
                    }
                }
            }

            public bool MoveCategoryFromMainToOther(string category)
            {
                if (TabCategories["Other"].Contains(category))
                {
                    Log("(Error) Tried to move inventory category from 'Main' to 'Other',"
                        + $" but TabCategories already contains the category '{category}'");
                    return false;
                }
                TabCategories["Other"].Add(category);
                Holder.RequirePart<QudUX_InventoryScreenState>().CategoriesInOtherTab.Add(category);
                UpdateCategoryMappings();
                return true;
            }

            public bool MoveCategoryFromOtherToMain(string category)
            {
                if (!TabCategories["Other"].Contains(category))
                {
                    Log("(Error) Tried to move inventory category from 'Other' to 'Main',"
                        + $" but TabCategories didn't contain the category '{category}'");
                    return false;
                }
                TabCategories["Other"].Remove(category);
                UpdateCategoryMappings();
                return true;
            }

            public bool CurrentTabIncludes(string category)
            {
                bool isMappedToTab = CategoryToTab.TryGetValue(category, out string catTab);
                if (CurrentTab == "Main")
                {
                    return (isMappedToTab == false); //Main holds everything thats not mapped to another tab
                }
                return isMappedToTab && (catTab == CurrentTab);
            }

            public string GetTabUIString()
            {
                string tabBar = "{{y| ";
                for (int i = 0; i < Tabs.Count; i++)
                {
                    if (i == TabIndex)
                    {
                        tabBar += "> {{W|" + Tabs[i] + "}}   ";
                    }
                    else
                    {
                        tabBar += "  " + Tabs[i] + "   ";
                    }
                }
                tabBar += "}}";
                return tabBar;
            }
        }

        public static string GetItemValueString(GameObject item, bool shouldHighlight = false)
        {
            string valueString;
            int weight = (item.pPhysics != null) ? item.pPhysics.Weight : 0;
            if (weight <= 0)
            {
                if (shouldHighlight)
                {
                    valueString = "{{Y|weightless}}";
                }
                else
                {
                    valueString = "{{K|weightless}}";
                }
            }
            else
            {
                double itemValue = GetItemPricePer(item) * (double)item.Count;
                double perPoundValue = itemValue / (double)weight;
                int finalValue = (int)Math.Round(perPoundValue, MidpointRounding.AwayFromZero);
                if (shouldHighlight)
                {
                    valueString = "{{Y|{{B|$}}{{C|" + finalValue + "}} / lb.}}";
                }
                else
                {
                    valueString = "{{y|{{b|$}}{{c|" + finalValue + "}} / lb.}}";
                }
            }
            return valueString;
        }

        public static double GetItemPricePer(GameObject item)
        {
            return item.ValueEach * AdjustedCopyOf_TradeUI_GetMultiplier(item);
        }

        public static float AdjustedCopyOf_TradeUI_GetMultiplier(GameObject item)
        {
            if (item != null && item.GetIntProperty("Currency") != 0)
            {
                return 1f;
            }
            GameObject body = XRLCore.Core.Game.Player.Body;
            if (!body.Statistics.ContainsKey("Ego"))
            {
                return 0.25f;
            }
            float num = body.StatMod("Ego");
            if (body.HasPart("Persuasion_SnakeOiler"))
            {
                num += 2f;
            }
            if (body.HasEffect("Glotrot"))
            {
                num = -3f;
            }
            float num2 = 0.35f + 0.07f * num;
            if (body.HasPart("SociallyRepugnant"))
            {
                num2 /= 5f;
            }
            if (num2 > 0.95f)
            {
                num2 = 0.95f;
            }
            else if (num2 < 0.05f)
            {
                num2 = 0.05f;
            }
            return num2;
        }
    }
}
