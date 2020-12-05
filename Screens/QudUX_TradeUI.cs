using ConsoleLib.Console;
using System.Collections.Generic;
using System;
using System.Linq;
using XRL.Language;
using XRL.World.Parts.Skill;
using XRL.World.Parts;
using XRL.World;

namespace XRL.UI.QudUX_DummyTradeNamespace
{

    public class TradeEntry
    {

        public int NumberSelected = 0;
        public GameObject GO = null;
        public string CategoryName = "";

        public TradeEntry(string CategoryName)
        {
            this.CategoryName = CategoryName;
        }

        public TradeEntry(GameObject GO)
        {
            this.GO = GO;
        }

    }

    public class CategorizedTradeEntries
    {

        public List<TradeEntry> TradeEntries;
        private List<int> _IndexMap;
        private List<string> _ActiveCategoryFilters;
        private bool PlayerTrader;
        private TradeUI.TradeScreenMode TradeMode;
        private Dictionary<string, bool> _NonPlayerExpandState = new Dictionary<string, bool>();
        private static Dictionary<string, bool> _PlayerExpandStateTrader = new Dictionary<string, bool>();
        private static Dictionary<string, bool> _PlayerExpandStateContainer = new Dictionary<string, bool>();
        private Dictionary<string, bool> CategoryExpandState
        {
            get
            {
                if (!PlayerTrader)
                {
                    return _NonPlayerExpandState;
                }
                else
                {
                    return (TradeMode == TradeUI.TradeScreenMode.Trade) ? _PlayerExpandStateTrader : _PlayerExpandStateContainer;
                }
            }
        }

        public CategorizedTradeEntries(bool isPlayerSource) : this(new List<TradeEntry>(), isPlayerSource, TradeUI.TradeScreenMode.Trade) { }

        public CategorizedTradeEntries(List<TradeEntry> categorySortedEntries, bool isPlayerSource, TradeUI.TradeScreenMode mode)
        {
            PlayerTrader = isPlayerSource;
            TradeMode = mode;
            TradeEntries = categorySortedEntries;
            _ActiveCategoryFilters = new List<string>();
            _IndexMap = new List<int>();
            Update();
        }

        public void Clear(bool resetNonPlayerExpandState = true)
        {
            TradeEntries.Clear();
            _IndexMap.Clear();
            if (!PlayerTrader)
            {
                if (resetNonPlayerExpandState)
                {
                    CategoryExpandState.Clear();
                }
            }
        }

        public void Update()
        {
            _IndexMap.Clear();
            bool bExpanded = true;
            bool bFiltered = false;
            string currentCategory = string.Empty;
            for (int i = 0; i < TradeEntries.Count; i++)
            {
                TradeEntry entry = TradeEntries[i];
                if (!string.IsNullOrEmpty(entry.CategoryName) && currentCategory != entry.CategoryName)
                {
                    currentCategory = entry.CategoryName;
                    if (!CategoryExpandState.ContainsKey(currentCategory))
                    {
                        CategoryExpandState[currentCategory] = true;
                    }
                    bExpanded = CategoryExpandState[currentCategory];
                    bFiltered = _ActiveCategoryFilters.Contains(currentCategory);
                }
                if (!bFiltered)
                {
                    if (entry.GO == null || bExpanded)
                    {
                        _IndexMap.Add(i);
                    }
                    else if (entry.NumberSelected > 0)
                    {
                        //always show items with >0 selected, even if parent category is collapsed
                        _IndexMap.Add(i);
                    }
                }
            }
        }

        public void SetScreenMode(TradeUI.TradeScreenMode screenMode)
        {
            TradeMode = screenMode;
            Update();
        }

        public void SetCategoryFilter(string CategoryNames)
        {
            SetCategoryFilter(CategoryNames.Split(',').ToList());
        }

        public void SetCategoryFilter(List<string> CategoryList)
        {
            _ActiveCategoryFilters.Clear();
            foreach (string cat in CategoryList)
            {
                _ActiveCategoryFilters.Add(cat);
            }
        }

        public void ClearCategoryFilter()
        {
            _ActiveCategoryFilters.Clear();
        }

        public bool IsCategoryExpanded(string category)
        {
            bool result;
            if (CategoryExpandState.TryGetValue(category, out result))
            {
                return result;
            }
            return true;
        }

        public void ToggleCategoryExpansion(string category)
        {
            bool bExpanded;
            if (!CategoryExpandState.TryGetValue(category, out bExpanded))
            {
                bExpanded = true;
            }
            CategoryExpandState[category] = !bExpanded;
            Update();
        }

        public void ExpandAllCategories()
        {
            SetGlobalCategoryExpandState(true);
        }

        public void CollapseAllCategories()
        {
            SetGlobalCategoryExpandState(false);
        }

        private void SetGlobalCategoryExpandState(bool bState)
        {
            List<string> catKeys = new List<string>(CategoryExpandState.Keys);
            foreach (string catKey in catKeys)
            {
                CategoryExpandState[catKey] = bState;
            }
            Update();
        }

        public int FindObject(GameObject thing)
        {
            int internalIndex = TradeEntries.FindIndex(t => t.GO == thing);
            if (internalIndex < 0)
            {
                return -1; //item not present
            }
            int index = _IndexMap.FindIndex(i => i == internalIndex);
            if (index >= 0)
            {
                return index;
            }
            return -2; //item hidden by filter or collapsed category
        }

        public int FindCategory(string category)
        {
            int internalIndex = TradeEntries.FindIndex(t => t.CategoryName == category);
            if (internalIndex < 0)
            {
                return -1; //category not present
            }
            int index = _IndexMap.FindIndex(i => i == internalIndex);
            if (index >= 0)
            {
                return index;
            }
            return -2; //category hidden by filter
        }

        public void ClearTradeSelections()
        {
            TradeEntries.ForEach(item => item.NumberSelected = 0);
        }

        public int Count
        {
            get
            {
                return _IndexMap.Count;
            }
        }

        public TradeEntry this[int i]
        {
            get
            {
                if (i >= _IndexMap.Count)
                {
                    throw new IndexOutOfRangeException($"Index {i} is out of range for {this.GetType().Name}._IndexMap");
                }
                if (_IndexMap[i] >= TradeEntries.Count)
                {
                    throw new IndexOutOfRangeException($"Index {_IndexMap[i]} (_IndexMap[{i}]) is out of range for {this.GetType().Name}.TradeEntries");
                }
                return TradeEntries[_IndexMap[i]];
            }
        }

    }

    [UIView("Trade", ForceFullscreen: true, NavCategory: "Trade,Menu", UICanvas: null)]
    public class TradeUI : IWantsTextConsoleInit
    {

        public static TextConsole _TextConsole;
        public static ScreenBuffer _ScreenBuffer;
        public static double Performance = 1.0;

        void IWantsTextConsoleInit.Init(TextConsole console, ScreenBuffer buffer)
        {
            _TextConsole = console;
            _ScreenBuffer = buffer;
        }

        public static GameObject _Trader = null;

        public static bool AssumeTradersHaveWater = true;

        public static double GetMultiplier(GameObject GO)
        {

            if (GO == null || !GO.IsCurrency)
            {
                return Performance;
            }
            return 1.0;

        }

        public static bool ValidForTrade(GameObject obj, GameObject Trader, GameObject Other = null, TradeScreenMode ScreenMode = TradeScreenMode.Trade, float costMultiple = 1f)
        {
            if (Other != null && obj.MovingIntoWouldCreateContainmentLoop(Other))
            {
                return false;
            }
            if (ScreenMode == TradeScreenMode.Container)
            {
                return true;
            }
            if (obj.HasPropertyOrTag("questitem"))
            {
                return true;
            }
            if (obj.IsNatural())
            {
                return false;
            }
            if (costMultiple > 0 && obj.HasPropertyOrTag("WaterContainer"))
            {
                LiquidVolume LV = obj.LiquidVolume;
                if (LV != null && LV.IsFreshWater() && !obj.HasPart("TinkerItem"))
                {
                    return false;
                }
            }
            if (Trader.IsPlayer())
            {
                if (obj.HasPropertyOrTag("PlayerWontSell"))
                {
                    return false;
                }
            }
            else
            {
                if (obj.HasPropertyOrTag("WontSell"))
                {
                    return false;
                }
                if (Trader.HasPropertyOrTag("WontSell") && Trader.GetPropertyOrTag("WontSell").Contains(obj.Blueprint))
                {
                    return false;
                }
                if (Trader.HasPropertyOrTag("WontSellTag") && obj.HasTagOrProperty(Trader.GetPropertyOrTag("WontSellTag")))
                {
                    return false;
                }
            }
            return true;
        }

        public static void GetObjects(GameObject Trader, CategorizedTradeEntries ReturnObjects, TradeScreenMode screenMode, GameObject Other, float costMultiple = 1f)
        {
            List<GameObject> Objects = new List<GameObject>(64);
            foreach (GameObject GO in Trader.GetPart<Inventory>().GetObjects())
            {
                if (ValidForTrade(GO, Trader, Other, screenMode, costMultiple))
                {
                    Objects.Add(GO);
                }
            }
            Objects.Sort(new SortGOCategory());
            string CurrentCategory = "";
            foreach (GameObject GO in Objects)
            {
                GO.Seen();
                string Cat = GO.GetInventoryCategory();
                if (Cat != CurrentCategory)
                {
                    CurrentCategory = Cat;
                    ReturnObjects.TradeEntries.Add(new TradeEntry(CurrentCategory));
                }
                ReturnObjects.TradeEntries.Add(new TradeEntry(GO));
            }
            ReturnObjects.Update();
        }

        public static string FormatPrice(double Price, float multiplier)
        {
            return String.Format("{0:0.00}", (Price * multiplier));
        }

        public enum TradeScreenMode
        {
            Trade,
            Container
        }

        public static int[] ScrollPosition = new int[2];
        public static double[] Totals = new double[2];
        public static int[] Weight = new int[2];
        public static CategorizedTradeEntries[] Objects = null;
        public static int nTotalWeight = 0;
        public static int nMaxWeight = 0;

        public static void Reset(TradeScreenMode screenMode, bool resetNonPlayerExpandState = true)
        {
            ScrollPosition = new int[2];
            Totals = new double[2];
            Weight = new int[2];

            if (Objects == null)
            {
                Objects = new CategorizedTradeEntries[2];
                Objects[0] = new CategorizedTradeEntries(isPlayerSource: false);
                Objects[1] = new CategorizedTradeEntries(isPlayerSource: true);
            }
            Objects[0].Clear(resetNonPlayerExpandState);
            Objects[1].Clear();
            Objects[0].SetScreenMode(screenMode);
            Objects[1].SetScreenMode(screenMode);
        }

        public static int GetSideOfObject(GameObject obj)
        {
            if (Objects[0].FindObject(obj) != -1)
            {
                return 0;
            }
            return 1;
        }

        public static double ItemValueEach(GameObject obj, bool? TraderInventory = null)
        {
            double result = obj.ValueEach;
            if (_Trader != null && (TraderInventory == true || (TraderInventory == null && Objects[0].FindObject(obj) != -1)))
            {
                int MinimumValue = _Trader.GetIntProperty("MinimumSellValue");
                if (MinimumValue > 0 && result < MinimumValue)
                {
                    result = (double)MinimumValue;
                }
            }
            return result;
        }

        public static double GetValue(GameObject obj, bool? TraderInventory = null)
        {
            if (TraderInventory == true || (TraderInventory == null && Objects[0].FindObject(obj) != -1))
            {
                return ItemValueEach(obj, true) / GetMultiplier(obj);
            }
            if (TraderInventory == false || (TraderInventory == null && Objects[1].FindObject(obj) != -1))
            {
                return ItemValueEach(obj, false) * GetMultiplier(obj);
            }
            return 0;
        }

        public static int GetNumberSelected(GameObject obj)
        {
            int n = Objects[0].FindObject(obj);
            if (n > -1)
            {
                return Objects[0][n].NumberSelected;
            }
            n = Objects[1].FindObject(obj);
            if (n > -1)
            {
                return Objects[1][n].NumberSelected;
            }
            return -999;
        }

        public static void SetSelectedObject(GameObject obj)
        {
            int n = Objects[0].FindObject(obj);
            if (n > -1)
            {
                SideSelected = 0;
                RowSelect = n;
            }

            n = Objects[1].FindObject(obj);
            if (n > -1)
            {
                SideSelected = 1;
                RowSelect = n;
            }
        }

        public static void SetNumberSelected(GameObject obj, int amount)
        {
            int n = Objects[0].FindObject(obj);
            if (n > -1)
            {
                Objects[0][n].NumberSelected = amount;
            }
            n = Objects[1].FindObject(obj);
            if (n > -1)
            {
                Objects[1][n].NumberSelected = amount;
            }
            UpdateTotals();
        }

        public static void PerformObjectDropped(GameObject Object, int DroppedOnSide)
        {
            int n;
            if ((n = Objects[DroppedOnSide].FindObject(Object)) > -1)
            {
                Objects[DroppedOnSide][n].NumberSelected = 0;
                UpdateTotals();
            }
            else if ((n = Objects[1 - DroppedOnSide].FindObject(Object)) > -1)
            {
                Objects[1 - DroppedOnSide][n].NumberSelected = Objects[1 - DroppedOnSide][n].GO.Count;
                UpdateTotals();
            }
        }

        public static void UpdateTotals()
        {
            for (int sideSelected = 0; sideSelected <= 1; sideSelected++)
            {
                double nMultiplier = 1.0f;
                if (sideSelected == 0)
                {
                    nMultiplier = 1.0 / Performance;
                }
                if (sideSelected == 1)
                {
                    nMultiplier = Performance;
                }
                Totals[sideSelected] = 0;
                Weight[sideSelected] = 0;
                for (int x = 0; x < Objects[sideSelected].Count; x++)
                {
                    if (Objects[sideSelected][x].GO != null)
                    {
                        if (Objects[sideSelected][x].NumberSelected > 0)
                        {
                            Weight[sideSelected] += (Objects[sideSelected][x].GO.WeightEach * Objects[sideSelected][x].NumberSelected);

                            if (Objects[sideSelected][x].GO.GetIntProperty("Currency") != 0)
                            {
                                Totals[sideSelected] += ItemValueEach(Objects[sideSelected][x].GO) * Objects[sideSelected][x].NumberSelected;
                            }
                            else
                            {
                                Totals[sideSelected] += ItemValueEach(Objects[sideSelected][x].GO) * nMultiplier * Objects[sideSelected][x].NumberSelected;
                            }
                        }
                    }
                }
                Totals[sideSelected] *= costMultiple;
            }

            sReadout = " {{C|" + String.Format("{0:0.###}", Totals[0]) + "}} drams <-> {{C|" + String.Format("{0:0.###}", Totals[1]) + "}} drams " + (char)196 + (char)196 + " {{W|$" + Core.XRLCore.Core.Game.Player.Body.GetFreeDrams() + "}} ";
        }

        public static CategorizedTradeEntries CurrentSide
        {
            get
            {
                return Objects[SideSelected];
            }
        }

        public static TradeEntry CurrentSelection
        {
            get
            {
                return Objects[SideSelected][RowSelect + ScrollPosition[SideSelected]];
            }
        }

        static int SideSelected = 0;
        static int RowSelect = 0;

        public static string sReadout = "";
        public static float costMultiple = 1;
        public static void ShowTradeScreen(GameObject Trader, float _costMultiple = 1f, TradeScreenMode screenMode = TradeScreenMode.Trade)
        {
            bool isCompanion = Trader.IsPlayerLed();
            if (isCompanion)
            {
                _costMultiple = 0f;
            }
            costMultiple = _costMultiple;
            TextConsole.LoadScrapBuffers();
            GameManager.Instance.PushGameView("Trade");
            Reset(screenMode);

        top:
            if (Trader == null || !Trader.HasPart("Inventory"))
            {
                GameManager.Instance.PopGameView();
                Reset(screenMode, false);
                return;
            }
            _Trader = Trader;

            Performance = GetTradePerformanceEvent.GetFor(IComponent<GameObject>.ThePlayer, _Trader);
            SideSelected = 0;
            RowSelect = 0;

            int ObjectsToShow = 21;
            int YStart = 1;

        refresh:
            Objects[0].Clear(false);
            Objects[1].Clear();

            ScrollPosition[0] = 0;
            ScrollPosition[1] = 0;
            Totals[0] = 0;
            Totals[1] = 0;

            GetObjects(Trader, Objects[0], screenMode, XRL.Core.XRLCore.Core.Game.Player.Body, costMultiple);
            GetObjects(XRL.Core.XRLCore.Core.Game.Player.Body, Objects[1], screenMode, Trader, costMultiple);

            if (XRL.Core.XRLCore.Core.Game.Player.Body.HasPart("Inventory") && XRL.Core.XRLCore.Core.Game.Player.Body.HasPart("Body"))
            {
                Inventory pInventory = XRL.Core.XRLCore.Core.Game.Player.Body.GetPart("Inventory") as Inventory;
                Body pBody = XRL.Core.XRLCore.Core.Game.Player.Body.GetPart<Body>();
                nTotalWeight = pInventory.GetWeight() + pBody.GetWeight();
                nMaxWeight = Rules.Stats.GetMaxWeight(XRL.Core.XRLCore.Core.Game.Player.Body);
            }

            if (Objects[0].Count <= 0 && costMultiple > 0)
            {
                Popup.Show(Trader.The + Trader.DisplayNameOnly + Trader.GetVerb("have") + " nothing to trade.");
                _Trader = null;
                GameManager.Instance.PopGameView();
                Reset(screenMode);
                return;
            }

            UpdateTotals();

            if (Options.OverlayPrereleaseTrade)
            {
                TradeView.instance.QueueInventoryUpdate();
            }

            bool TradingDone = false;
            int NumInput = 0;

            bool bDone = false;
            while (!bDone)
            {
                if (CurrentSide.Count == 0)
                {
                    SideSelected = 1 - SideSelected;
                }
                Event.ResetPool(resetMinEventPools: false);
                _ScreenBuffer.Clear();

                string Price;
                string SelectedItemText = "";
                string SelectedItemWeightAndPrice = "";
                IRenderable renderInfo = null;

                for (int x = 0, p = ScrollPosition[0]; x < ObjectsToShow && p < Objects[0].Count; x++, p++)
                {
                    _ScreenBuffer.Goto(2, x + YStart);

                    GameObject obj = Objects[0][p].GO;
                    if (obj != null)
                    {
                        if (Objects[0][p].NumberSelected > 0)
                        {
                            _ScreenBuffer.Write("{{&Y^g|" + Objects[0][p].NumberSelected + "}} ");
                        }
                        _ScreenBuffer.Write(obj.RenderForUI());
                        _ScreenBuffer.Write(" ");
                        _ScreenBuffer.Write(obj.DisplayName);

                        Price = "";
                        if (SideSelected == 0 && RowSelect == x)
                        {
                            SelectedItemText = obj.DisplayName;
                            SelectedItemWeightAndPrice = " {{K|" + obj.Weight + "#}}";
                            renderInfo = obj.RenderForUI();
                            if (screenMode == TradeScreenMode.Trade)
                            {
                                string sColor = "B";
                                if (obj.GetIntProperty("Currency") != 0)
                                {
                                    sColor = "Y";
                                }
                                Price = "{{" + sColor + "|$}}{{C|" + FormatPrice(GetValue(obj, true), costMultiple) + "}}";
                                SelectedItemWeightAndPrice += " " + Price;
                            }
                        }
                        else
                        {
                            if (screenMode == TradeScreenMode.Trade)
                            {
                                string sColor = "b";
                                if (obj.GetIntProperty("Currency") != 0)
                                {
                                    sColor = "W";
                                }
                                Price = "{{" + sColor + "|$}}{{c|" + FormatPrice(GetValue(obj, true), costMultiple) + "}}";
                            }
                        }

                        int Position = 40 - ColorUtility.LengthExceptFormatting(Price);
                        _ScreenBuffer.Goto(Position, x + YStart);
                        _ScreenBuffer.Write(Price);
                    }
                    else
                    {
                        string expanderColor = (SideSelected == 0 && RowSelect == x) ? "y" : "K";
                        string expanderChar = (Objects[0].IsCategoryExpanded(Objects[0][p].CategoryName) == true) ? "-" : "+";
                        string catPostfix = "{{" + expanderColor + "|[" + expanderChar + "]}}";
                        string s = "{{K|[{{y|" + Objects[0][p].CategoryName + "}}]}}" + catPostfix;
                        _ScreenBuffer.Goto(40 - ColorUtility.LengthExceptFormatting(s), x + YStart);
                        _ScreenBuffer.Write(s);
                    }
                }

                _ScreenBuffer.Fill(41, YStart, 77, YStart + ObjectsToShow, ' ', 0);

                for (int x = 0, p = ScrollPosition[1]; x < ObjectsToShow && p < Objects[1].Count; x++, p++)
                {
                    _ScreenBuffer.Goto(42, x + YStart);

                    if (Objects[1][p].GO != null)
                    {
                        if (Objects[1][p].NumberSelected > 0)
                        {
                            _ScreenBuffer.Write("{{&Y^g|" + Objects[1][p].NumberSelected.ToString() + "}} ");
                        }
                        _ScreenBuffer.Write(Objects[1][p].GO.RenderForUI());
                        _ScreenBuffer.Write(" ");
                        _ScreenBuffer.Write(Objects[1][p].GO.DisplayName);

                        Price = "";
                        if (SideSelected == 1 && RowSelect == x)
                        {
                            renderInfo = Objects[1][p].GO.RenderForUI();
                            SelectedItemText = Objects[1][p].GO.DisplayName;
                            SelectedItemWeightAndPrice = " {{K|" + Objects[1][p].GO.Weight + "#}}";
                            if (screenMode == TradeScreenMode.Trade)
                            {
                                string sColor = "B";
                                if (Objects[1][p].GO.GetIntProperty("Currency") != 0)
                                {
                                    sColor = "Y";
                                }
                                Price = "{{" + sColor + "|$}}{{C|" + FormatPrice(GetValue(Objects[1][p].GO, false), costMultiple) + "}}";
                                SelectedItemWeightAndPrice += " " + Price;
                            }
                        }
                        else
                        {
                            if (screenMode == TradeScreenMode.Trade)
                            {
                                string sColor = "b";
                                if (Objects[1][p].GO.GetIntProperty("Currency") != 0)
                                {
                                    sColor = "W";
                                }
                                Price = "{{" + sColor + "|$}}{{c|" + FormatPrice(GetValue(Objects[1][p].GO, false), costMultiple) + "}}";
                            }
                        }

                        int Position = 79 - ColorUtility.LengthExceptFormatting(Price);
                        _ScreenBuffer.Goto(Position, x + YStart);
                        _ScreenBuffer.Write(Price);
                    }
                    else
                    {
                        string expanderColor = (SideSelected == 1 && RowSelect == x) ? "y" : "K";
                        string expanderChar = (Objects[1].IsCategoryExpanded(Objects[1][p].CategoryName) == true) ? "-" : "+";
                        string catPostfix = "{{" + expanderColor + "|[" + expanderChar + "]}}";
                        string s = "{{K|[{{y|" + Objects[1][p].CategoryName + "}}]}}" + catPostfix;
                        _ScreenBuffer.Goto(79 - ColorUtility.LengthExceptFormatting(s), x + YStart);
                        _ScreenBuffer.Write(s);
                    }
                }

                _ScreenBuffer.SingleBox(0, 0, 79, 24, ColorUtility.MakeColor(TextColor.Grey, TextColor.Black));

                _ScreenBuffer.Goto(2, 0);
                _ScreenBuffer.Write("[ {{W|" + ColorUtility.CapitalizeExceptFormatting(Grammar.MakePossessive(Trader.DisplayNameOnly)) + " inventory}} ]");


                _ScreenBuffer.Goto(42, 0);
                _ScreenBuffer.Write("[ {{W|Your inventory}} ]");

                _ScreenBuffer.Goto(40, 0);
                _ScreenBuffer.Write(194);

                for (int x = 1; x < 22; x++)
                {
                    _ScreenBuffer.Goto(40, x);
                    _ScreenBuffer.Write(179);
                }

                for (int x = 1; x < 79; x++)
                {
                    _ScreenBuffer.Goto(x, 22);
                    _ScreenBuffer.Write(196);
                }

                if (SideSelected == 0)
                {
                    _ScreenBuffer.Goto(1, RowSelect + YStart);
                }
                else
                {
                    _ScreenBuffer.Goto(41, RowSelect + YStart);
                }

                _ScreenBuffer.Write("{{&k^Y|>}}");

                _ScreenBuffer.Goto(40, 22);
                _ScreenBuffer.Write(193);

                _ScreenBuffer.Goto(0, 22);
                _ScreenBuffer.Write(195);
                _ScreenBuffer.Goto(79, 22);
                _ScreenBuffer.Write(180);

                if (CurrentSide.Count > 0 && CurrentSelection != null)
                {
                    _ScreenBuffer.Goto(2, 23);
                    if (renderInfo != null)
                    {
                        _ScreenBuffer.Write(renderInfo);
                        _ScreenBuffer.Goto(4, 23);
                    }
                    _ScreenBuffer.Write(SelectedItemText);
                    if (!string.IsNullOrEmpty(SelectedItemWeightAndPrice))
                    {
                        _ScreenBuffer.Goto(79 - ColorUtility.LengthExceptFormatting(SelectedItemWeightAndPrice), 23);
                        _ScreenBuffer.Write(SelectedItemWeightAndPrice);
                    }
                }

                _ScreenBuffer.Goto(2, 24);
                _ScreenBuffer.Write("[{{W|ESC}} - Exit]");

                _ScreenBuffer.Goto(15, 24);
                _ScreenBuffer.Write("[{{W|+}}/{{W|-}} Add/Remove]");
                _ScreenBuffer.Goto(32, 24);
                _ScreenBuffer.Write("[{{W|0-9}} Pick]");
                if (screenMode == TradeScreenMode.Trade)
                {
                    _ScreenBuffer.Goto(43, 24);
                    _ScreenBuffer.Write("[{{W|o}} Offer]");
                }
                else
                {
                    _ScreenBuffer.Goto(43, 24);
                    _ScreenBuffer.Write("[{{W|o}} Transfer]");
                }

                _ScreenBuffer.Goto(55, 24);
                _ScreenBuffer.Write("[{{W|Space}} Actions]");

                if (screenMode == TradeScreenMode.Trade)
                {
                    _ScreenBuffer.Goto(3, 22);
                    _ScreenBuffer.Write(" {{W|$" + Trader.GetFreeDrams() + "}} ");

                    string sDrams = " {{C|" + String.Format("{0:0.###}", Totals[0]) + "}} drams ";
                    _ScreenBuffer.Goto(39 - ColorUtility.LengthExceptFormatting(sDrams), 22);
                    _ScreenBuffer.Write(sDrams);

                    _ScreenBuffer.Goto(42, 22);
                    _ScreenBuffer.Write(" {{C|" + String.Format("{0:0.###}", Totals[1]) + "}} drams " + (char)196 + (char)196 + " {{W|$" + Core.XRLCore.Core.Game.Player.Body.GetFreeDrams() + "}} ");
                }

                for (int side = 0; side <= 1; side++)
                {
                    if (Objects[side].Count > ObjectsToShow)
                    {
                        for (int y = 1; y < 22; y++)
                        {
                            if (side == 0)
                            {
                                _ScreenBuffer.Goto(0, y);
                            }
                            else
                            {
                                _ScreenBuffer.Goto(79, y);
                            }
                            _ScreenBuffer.Write(177, ColorUtility.Bright((ushort)TextColor.Black), (ushort)TextColor.Black);
                        }

                        int DisplayPages = (int)Math.Ceiling((double)Objects[side].Count / (double)ObjectsToShow);
                        int DisplayPagesSmall = ((int)Math.Ceiling((double)(Objects[side].Count + ObjectsToShow) / (double)ObjectsToShow));
                        if (DisplayPages <= 0)
                        {
                            DisplayPages = 1;
                        }
                        if (DisplayPagesSmall <= 0)
                        {
                            DisplayPagesSmall = 1;
                        }

                        int CursorSize = 21 / DisplayPagesSmall;
                        if (CursorSize <= 0)
                        {
                            CursorSize = 1;
                        }

                        int CursorStart = (int)((double)(21 - CursorSize) * ((double)ScrollPosition[side] / (double)(Objects[side].Count - ObjectsToShow)));
                        CursorStart++;

                        for (int y = CursorStart; y < CursorStart + CursorSize; y++)
                        {
                            if (side == 0)
                            {
                                _ScreenBuffer.Goto(0, y);
                            }
                            else
                            {
                                _ScreenBuffer.Goto(79, y);
                            }
                            _ScreenBuffer.Write(219, ColorUtility.Bright((ushort)TextColor.Grey), (ushort)TextColor.Black);
                        }
                    }
                }

                string WeightColor = "K";
                if (nTotalWeight + Weight[0] - Weight[1] > nMaxWeight)
                {
                    WeightColor = "R";
                }

                string sWeight = " {{" + WeightColor + "|" + (nTotalWeight + Weight[0] - Weight[1]) + "/" + nMaxWeight + " lbs.}} ";
                _ScreenBuffer.Goto(77 - ColorUtility.LengthExceptFormatting(sWeight), 22);
                _ScreenBuffer.Write(sWeight);

                _TextConsole.DrawBuffer(_ScreenBuffer, null, Options.OverlayPrereleaseTrade);
                Keys c = Keyboard.getvk(Options.MapDirectionsToKeypad, true);

                if (c == Keys.Escape)
                {
                    bDone = true;
                }
                if (c == Keys.MouseEvent && Keyboard.CurrentMouseEvent.Event == "RightClick")
                {
                    bDone = true;
                }

                if (c >= Keys.D0 && c <= Keys.D9)
                {
                    if (CurrentSide.Count > 0)
                    {
                        GameObject GO = CurrentSelection.GO;
                        if (GO != null)
                        {
                            int Num = (c - Keys.D0);
                            if (NumInput >= GO.Count)
                            {
                                NumInput = Num;
                            }
                            else
                            {
                                NumInput = (NumInput * 10) + Num;
                            }
                            if (NumInput > GO.Count)
                            {
                                CurrentSelection.NumberSelected = GO.Count;
                            }
                            else
                            {
                                CurrentSelection.NumberSelected = NumInput;
                            }
                        }
                        UpdateTotals();
                        continue;
                    }
                }
                else
                {
                    NumInput = 0;
                }

                if (c == Keys.Oemtilde)
                {
                    if (CurrentSide.Count > 0)
                    {
                        CurrentSelection.NumberSelected = 0;
                        UpdateTotals();
                        continue;
                    }
                }

                if (Keyboard.vkCode == Keys.Space)
                {
                    if (CurrentSide.Count > 0)
                    {
                        GameObject GO = CurrentSelection.GO;
                        if (GO != null)
                        {
                            List<string> choiceList = new List<string>
                            {
                                "Look"
                            };

                            List<char> hotkeyList = new List<char>
                            {
                                'l'
                            };

                            if (Tinkering.GetIdentifyLevel(Trader) > 0)
                            {
                                choiceList.Add("Identify");
                                hotkeyList.Add('i');
                            }

                            if (Trader.HasSkill("Tinkering_Repair"))
                            {
                                choiceList.Add("Repair");
                                hotkeyList.Add('r');
                            }

                            if (Trader.HasSkill("Tinkering_Tinker1"))
                            {
                                choiceList.Add("Recharge");
                                hotkeyList.Add('c');
                            }

                            if (Trader.GetIntProperty("Librarian") != 0)
                            {
                                choiceList.Add("Read");
                                hotkeyList.Add('b');
                            }

                            int ichoice = Popup.ShowOptionList("", choiceList.ToArray(), hotkeyList.ToArray(), 0, "select an action", 60, false, true);

                            if (ichoice >= 0)
                            {
                                if (choiceList[ichoice] == "Identify")
                                {
                                    Keyboard.vkCode = Keys.I;
                                }
                                else
                                if (choiceList[ichoice] == "Read")
                                {
                                    Keyboard.vkCode = Keys.B;
                                }
                                else
                                if (choiceList[ichoice] == "Repair")
                                {
                                    Keyboard.vkCode = Keys.R;
                                }
                                else
                                if (choiceList[ichoice] == "Look")
                                {
                                    Keyboard.vkCode = Keys.L;
                                }
                                else
                                if (choiceList[ichoice] == "Recharge")
                                {
                                    Keyboard.vkCode = Keys.C;
                                }
                            }
                        }
                        else
                        {
                            CurrentSide.ToggleCategoryExpansion(CurrentSelection.CategoryName);
                        }
                    }
                }

                if (Keyboard.vkCode == Keys.R)
                {
                    if (Trader.HasSkill("Tinkering_Repair"))
                    {
                        if (CurrentSide.Count > 0)
                        {
                            GameObject GO = CurrentSelection.GO;

                            if (GO != null)
                            {
                                bool Multiple = GO.IsPlural || GO.Count > 1;
                                if (World.Parts.Skill.Tinkering_Repair.IsRepairable(GO))
                                {
                                    if (!World.Parts.Skill.Tinkering_Repair.IsRepairableBy(GO, Trader))
                                    {
                                        Popup.ShowBlock((Multiple ? "These items are" : "This item is") + " too complex for " + Trader.the + Trader.ShortDisplayName + " to repair.");
                                    }
                                    else
                                    {
                                        int cost = Math.Max(5 + (int)(GetValue(GO, false) / 25), 5) * GO.Count;
                                        if (Core.XRLCore.Core.Game.Player.Body.GetFreeDrams() < cost)
                                        {
                                            Popup.Show("You need {{C|" + cost + "}} " + (cost == 1 ? "dram" : "drams") + " of fresh water to repair " + (Multiple ? "those" : "that") + ".");
                                        }
                                        else
                                        if (Popup.ShowYesNo("You may repair " + (Multiple ? "those" : "this") + " for {{C|" + cost + "}} " + (cost == 1 ? "dram" : "drams") + " of fresh water.") == DialogResult.Yes)
                                        {
                                            if (Core.XRLCore.Core.Game.Player.Body.UseDrams(cost))
                                            {
                                                Trader.GiveDrams(cost);
                                                World.Parts.Skill.Tinkering_Repair.RepairObject(GO);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    Popup.ShowBlock((Multiple ? "Those items aren't" : "That item isn't") + " broken!");
                                }
                            }
                        }
                    }
                    else
                    {
                        Popup.Show("This trader doesn't have the skill to repair items.");
                    }
                    continue;
                }

                if (Keyboard.vkCode == Keys.Tab)
                {
                    bool anyUnselected = false;
                    for (int i = 0, j = CurrentSide.Count; i < j; i++)
                    {
                        GameObject GO = CurrentSide[i].GO;
                        if (GO != null)
                        {
                            int num = GO.Count;
                            if (CurrentSide[i].NumberSelected != num)
                            {
                                CurrentSide[i].NumberSelected = num;
                                anyUnselected = true;
                            }
                        }
                    }
                    if (!anyUnselected)
                    {
                        CurrentSide.ClearTradeSelections();
                    }
                    UpdateTotals();
                    continue;
                }

                if (Keyboard.vkCode == Keys.L)
                {
                    if (CurrentSide.Count > 0)
                    {
                        GameObject GO = CurrentSelection.GO;
                        InventoryActionEvent.Check(GO, Core.XRLCore.Core.Game.Player.Body, GO, "Look");
                    }
                    continue;
                }

                if (Keyboard.vkCode == Keys.C)
                {
                    if (Trader.HasSkill("Tinkering_Tinker1"))
                    {
                        if (CurrentSide.Count > 0)
                        {
                            GameObject GO = CurrentSelection.GO;
                            if (GO != null)
                            {
                                if (RechargeAction(GO, Trader))
                                {
                                    goto refresh;
                                }
                            }
                        }
                        else
                        {
                            Popup.Show("This trader doesn't have the skill to recharge items.");
                        }
                    }
                    continue;
                }

                if (Keyboard.vkCode == Keys.B && Trader.GetIntProperty("Librarian") != 0)
                {
                    if (CurrentSide.Count > 0)
                    {
                        GameObject GO = CurrentSelection.GO;
                        InventoryActionEvent.Check(GO, Core.XRLCore.Core.Game.Player.Body, GO, "Read");
                    }
                    continue;
                }

                if (Keyboard.vkCode == Keys.L)
                {
                    if (CurrentSide.Count > 0)
                    {
                        GameObject GO = CurrentSelection.GO;
                        string desc = null;
                        if (GO != null && GO.HasPart("Examiner") && !GO.Understood())
                        {
                            Examiner pExaminer = GO.GetPart("Examiner") as Examiner;
                            if (pExaminer != null)
                            {
                                desc = pExaminer.AlternateDescription;
                            }
                        }
                        else
                        if (GO != null)
                        {
                            Description pDescription = GO.GetPart("Description") as Description;
                            if (pDescription != null)
                            {
                                desc = pDescription.Short;
                            }
                        }
                        if (!string.IsNullOrEmpty(desc))
                        {
                            Popup.ShowBlock("[" + GO.DisplayName + "]\n\n" + desc + "\n");
                        }
                        else
                        {
                            Popup.ShowBlock("[" + GO.DisplayName + "]");
                        }
                    }
                    continue;
                }

                if (Keyboard.vkCode == Keys.I)
                {
                    if (CurrentSide.Count > 0)
                    {
                        int IdentifyLevel = Tinkering.GetIdentifyLevel(Trader);
                        if (IdentifyLevel > 0)
                        {
                            GameObject GO = CurrentSelection.GO;

                            if (GO != null)
                            {
                                if (GO.HasPart("Examiner"))
                                {
                                    Examiner pExaminer = GO.GetPart("Examiner") as Examiner;

                                    if (IdentifyLevel < pExaminer.Complexity)
                                    {
                                        Popup.ShowBlock("This item is too complex for " + Trader.the + Trader.DisplayNameOnly + " to identify.");
                                    }
                                    else
                                    {
                                        if (!GO.Understood())
                                        {
                                            int Cost = (int)Math.Pow(4, Math.Max(1, pExaminer.Complexity / 2f));

                                            if (Core.XRLCore.Core.Game.Player.Body.GetFreeDrams() < Cost)
                                            {
                                                Popup.ShowBlock("You do not have the required {{C|" + Cost + "}} " + (Cost == 1 ? "dram" : "drams") + " to identify this item.");
                                            }
                                            else
                                            {
                                                if (Popup.ShowYesNo("You may identify this for " + Cost + " " + (Cost == 1 ? "dram" : "drams") + " of fresh water.") == DialogResult.Yes)
                                                {
                                                    if (XRL.Core.XRLCore.Core.Game.Player.Body.UseDrams(Cost))
                                                    {
                                                        Trader.GiveDrams(Cost);
                                                        GO.MakeUnderstood();
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            Popup.ShowBlock("You already understand this item.");
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            Popup.ShowBlock(Trader.The + Trader.DisplayNameOnly + Trader.GetVerb("don't") + " have the skill to identify artifacts.");
                        }
                    }
                    continue;
                }

                if (Keyboard.vkCode == Keys.Enter)
                {
                    if (CurrentSide.Count > 0)
                    {
                        GameObject GO = CurrentSelection.GO;
                        if (GO != null)
                        {
                            if (CurrentSelection.NumberSelected == GO.Count)
                            {
                                CurrentSelection.NumberSelected = 0;
                            }
                            else
                            {
                                CurrentSelection.NumberSelected = GO.Count;
                            }
                            UpdateTotals();
                        }
                        else
                        {
                            CurrentSide.ToggleCategoryExpansion(CurrentSelection.CategoryName);
                        }
                    }
                }

                if (Keyboard.vkCode == Keys.Add || (c == Keys.NumPad9 && Keyboard.RawCode != Keys.PageUp && Keyboard.RawCode != Keys.Next) || c == Keys.Oemplus)
                {
                    if (CurrentSide.Count > 0)
                    {
                        GameObject GO = CurrentSelection.GO;

                        if (GO != null)
                        {
                            if (CurrentSelection.NumberSelected < CurrentSelection.GO.Count)
                            {
                                CurrentSelection.NumberSelected++;
                            }

                            UpdateTotals();
                        }
                        else
                        {
                            CurrentSide.ExpandAllCategories();
                        }
                    }
                }

                if (Keyboard.vkCode == Keys.Subtract || c == Keys.NumPad7 || c == Keys.OemMinus)
                {
                    if (CurrentSide.Count > 0)
                    {
                        GameObject GO = CurrentSelection.GO;
                        if (GO != null)
                        {
                            if (CurrentSelection.NumberSelected > 0)
                            {
                                CurrentSelection.NumberSelected--;
                            }

                            UpdateTotals();
                        }
                        else
                        {
                            string selectedCategory = CurrentSelection.CategoryName;
                            CurrentSide.CollapseAllCategories();
                            int newIndex = CurrentSide.FindCategory(selectedCategory);
                            RowSelect = Math.Max(0, newIndex);
                            ScrollPosition[SideSelected] = Math.Max(0, newIndex + 1 - ObjectsToShow);
                        }
                    }
                }

                if (c == Keys.NumPad4 && SideSelected == 1)
                {
                    if (Objects[0].Count > 0)
                    {
                        SideSelected = 0;
                    }
                }

                if (c == Keys.NumPad6 && SideSelected == 0)
                {
                    if (Objects[1].Count > 0)
                    {
                        SideSelected = 1;
                    }
                }

                if (c == Keys.NumPad8)
                {
                    if (RowSelect == 0)
                    {
                        if (ScrollPosition[SideSelected] > 0)
                        {
                            ScrollPosition[SideSelected]--;
                        }
                    }
                    else
                    {
                        RowSelect--;
                    }

                }

                if (c == Keys.NumPad2)
                {
                    if (RowSelect < (ObjectsToShow - 1) && RowSelect + ScrollPosition[SideSelected] < CurrentSide.Count - 1)
                    {
                        RowSelect++;
                    }
                    else
                    {
                        if (ScrollPosition[SideSelected] + ObjectsToShow < CurrentSide.Count)
                        {
                            ScrollPosition[SideSelected]++;
                        }
                    }
                }

                if (c == Keys.PageUp || Keyboard.RawCode == Keys.PageUp || Keyboard.RawCode == Keys.Back)
                {
                    if (RowSelect > 0)
                    {
                        RowSelect = 0;
                    }
                    else
                    {
                        ScrollPosition[SideSelected] -= (ObjectsToShow - 1);
                        if (ScrollPosition[SideSelected] < 0)
                        {
                            ScrollPosition[SideSelected] = 0;
                        }
                    }
                }

                if (c == Keys.PageDown || c == Keys.Next || Keyboard.RawCode == Keys.PageDown || Keyboard.RawCode == Keys.Next)
                {
                    if (RowSelect < (ObjectsToShow - 1))
                    {
                        RowSelect = (ObjectsToShow - 1);
                        if (RowSelect + ScrollPosition[SideSelected] >= CurrentSide.Count - 1)
                        {
                            RowSelect = CurrentSide.Count - 1 - ScrollPosition[SideSelected];
                        }
                    }
                    else
                        if (RowSelect == (ObjectsToShow - 1))
                    {
                        ScrollPosition[SideSelected] += (ObjectsToShow - 1);
                        if (RowSelect + ScrollPosition[SideSelected] >= CurrentSide.Count - 1)
                        {
                            ScrollPosition[SideSelected] = CurrentSide.Count - 1 - RowSelect;
                        }
                    }
                }

                if (RowSelect + ScrollPosition[SideSelected] >= CurrentSide.Count - 1)
                {
                    RowSelect = CurrentSide.Count - 1 - ScrollPosition[SideSelected];
                }

                if (c == Keys.O || c == Keys.F1)
                {
                    double fDifference = 0;
                    fDifference = Totals[0] - Totals[1];

                    int Difference = 0;
                    Difference = (int)Math.Ceiling(fDifference);

                    if (Difference > 0)
                    {
                        if (Core.XRLCore.Core.Game.Player.Body.GetFreeDrams() >= Difference)
                        {
                            if (Popup.ShowYesNo("You'll have to pony up " + Difference + " " + (Difference == 1 ? "dram" : "drams") + " of fresh water to even up the trade. Agreed?") == DialogResult.No)
                            {
                                goto next;
                            }
                        }
                        else
                        {
                            Popup.Show("You don't have " + Difference + " " + (Difference == 1 ? "dram" : "drams") + " of fresh water to even up the trade!");
                            goto next;
                        }
                    }

                    if (Difference < 0)
                    {
                        int Give = -Difference;
                        if (AssumeTradersHaveWater || Trader.GetFreeDrams() >= Give)
                        {
                            if (Popup.ShowYesNo(Trader.The + Trader.DisplayNameOnly + " will have to pony up " + Give + " " + (Give == 1 ? "dram" : "drams") + " of fresh water to even up the trade. Agreed?") == DialogResult.No)
                            {
                                goto next;
                            }
                        }
                        else
                        {
                            if (Popup.ShowYesNo(Trader.The + Trader.DisplayNameOnly + Trader.GetVerb("don't") + " have " + Give + " " + (Give == 1 ? "dram" : "drams") + " of fresh water to even up the trade! Do you want to complete the trade anyway?") != DialogResult.Yes)
                            {
                                goto next;
                            }
                        }
                    }

                    // BUG: If you trade a waterskin with this difference, you'll be able to double-up your value...

                    List<GameObject> TradeToTrader = new List<GameObject>(16);
                    List<GameObject> TradeToPlayer = new List<GameObject>(16);

                    if (Difference > 0)
                    {
                        XRL.Core.XRLCore.Core.Game.Player.Body.UseDrams(Difference);
                    }

                    for (int x = 0; x < Objects[0].Count; x++)
                    {
                        if (Objects[0][x].NumberSelected > 0)
                        {
                            GameObject GO = Objects[0][x].GO;
                            GO.SplitStack(Objects[0][x].NumberSelected, OwningObject: Trader);
                            if (!Trader.FireEvent(Event.New("CommandRemoveObject", "Object", GO).SetSilent(true)))
                            {
                                Popup.ShowBlock("Trade could not be completed, trader couldn't drop object: " + GO.DisplayName);
                                foreach (GameObject ReturnedGO in TradeToPlayer)
                                {
                                    Trader.ReceiveObject(ReturnedGO);
                                }
                                if (GameObject.validate(ref GO))
                                {
                                    GO.CheckStack();
                                }
                                goto refresh;
                            }

                            GO.RemoveIntProperty("_stock");
                            TradeToPlayer.Add(GO);
                        }
                    }

                    for (int x = 0; x < Objects[1].Count; x++)
                    {
                        if (Objects[1][x].NumberSelected > 0)
                        {
                            GameObject GO = Objects[1][x].GO;
                            GO.SplitStack(Objects[1][x].NumberSelected, OwningObject: XRL.Core.XRLCore.Core.Game.Player.Body);
                            if (!XRL.Core.XRLCore.Core.Game.Player.Body.FireEvent(Event.New("CommandRemoveObject", "Object", GO).SetSilent(true)))
                            {
                                Popup.ShowBlock("Trade could not be completed, you couldn't drop object: " + GO.DisplayName);
                                foreach (GameObject ReturnedGO in TradeToPlayer)
                                {
                                    Trader.ReceiveObject(ReturnedGO);
                                }
                                foreach (GameObject ReturnedGO in TradeToTrader)
                                {
                                    XRL.Core.XRLCore.Core.Game.Player.Body.ReceiveObject(ReturnedGO);
                                }
                                if (GameObject.validate(ref GO))
                                {
                                    GO.CheckStack();
                                }
                                goto refresh;
                            }

                            if (Trader.HasTagOrProperty("Merchant"))
                            {
                                GO.SetIntProperty("_stock", 1);
                            }
                            else
                            if (Trader.HasPart("Container"))
                            {
                                GO.SetIntProperty("StoredByPlayer", 1);
                            }
                            TradeToTrader.Add(GO);
                        }
                    }

                    if (Difference < 0)
                    {
                        int Payout = -Difference;
                        int Storable = Core.XRLCore.Core.Game.Player.Body.GetStorableDrams();

                        if (Storable < Payout)
                        {
                            UI.Popup.Show("You don't have enough water containers to carry that many drams!");
                            foreach (GameObject ReturnedGO in TradeToPlayer) Trader.ReceiveObject(ReturnedGO);
                            foreach (GameObject ReturnedGO in TradeToTrader) XRL.Core.XRLCore.Core.Game.Player.Body.ReceiveObject(ReturnedGO);
                            goto refresh;
                        }
                        else
                        {
                            Core.XRLCore.Core.Game.Player.Body.GiveDrams(Payout);
                            Trader.UseDrams(Payout);
                        }
                    }

                    if (screenMode == TradeScreenMode.Container)
                    {
                        foreach (GameObject GO in TradeToTrader)
                        {
                            Trader.FireEvent(Event.New("CommandTakeObject", "Object", GO, "PutBy", XRL.Core.XRLCore.Core.Game.Player.Body, "EnergyCost", 0));
                        }
                    }
                    else
                    {
                        foreach (GameObject GO in TradeToTrader)
                        {
                            Trader.TakeObject(GO, EnergyCost: 0);
                        }
                    }

                    foreach (GameObject GO in TradeToPlayer)
                    {
                        XRL.Core.XRLCore.Core.Game.Player.Body.TakeObject(GO, EnergyCost: 0);
                    }

                    if (Difference > 0)
                    {
                        Trader.GiveDrams(Difference);
                    }

                    if (screenMode == TradeScreenMode.Trade)
                    {
                        Popup.Show("Trade complete!");
                    }
                    goto top;
                }
            next:;
            }

            ScreenBuffer.ClearImposterSuppression();

            if (TradingDone)
            {
                XRL.Core.XRLCore.Core.Game.Player.Body.UseEnergy(1000, "Trading");
                _Trader.UseEnergy(1000, "Trading");
            }

            GameManager.Instance.PopGameView();
            _TextConsole.DrawBuffer(TextConsole.ScrapBuffer2, null, Options.OverlayPrereleaseTrade);
            _Trader = null;
            Reset(screenMode);
            if (isCompanion)
            {
                Trader.pBrain.PerformReequip();
            }
        }

        public static bool RechargeAction(GameObject GO, GameObject Trader)
        {
            bool AnyRelevant = false;
            bool AnyRechargeable = false;
            bool AnyNotFullyCharged = false;
            bool AnyRecharged = false;

            Predicate<IRechargeable> pProc = P =>
            {
                AnyRelevant = true;
                if (!P.CanBeRecharged())
                {
                    return true;
                }
                AnyRechargeable = true;
                int Charge = P.GetRechargeAmount();
                if (Charge <= 0)
                {
                    return true;
                }
                AnyNotFullyCharged = true;
                int cost = Math.Max(Charge / 500, 1);
                string indicate = (P.ParentObject.Count > 1) ? "one of those" : P.ParentObject.indicativeDistal;
                if (Core.XRLCore.Core.Game.Player.Body.GetFreeDrams() < cost)
                {
                    Popup.Show("You need {{C|" + Grammar.Cardinal(cost) + "}} " + (cost == 1 ? "dram" : "drams") + " of fresh water to charge " + indicate + ".");
                    return false;
                }
                else
                if (Popup.ShowYesNo("You may recharge " + indicate + " for {{C|" + Grammar.Cardinal(cost) + "}} " + (cost == 1 ? "dram" : "drams") + " of fresh water.") == DialogResult.Yes)
                {
                    if (Core.XRLCore.Core.Game.Player.Body.UseDrams(cost))
                    {
                        P.ParentObject.SplitFromStack();
                        P.AddCharge(Charge);
                        P.ParentObject.CheckStack();
                        Trader.GiveDrams(cost);
                        AnyRecharged = true;
                    }
                }
                return true;
            };

            GO.ForeachPartDescendedFrom<IRechargeable>(pProc);

            EnergyCellSocket pSocket = GO.GetPart<EnergyCellSocket>();
            if (pSocket != null && pSocket.Cell != null)
            {
                pSocket.Cell.ForeachPartDescendedFrom<IRechargeable>(pProc);
            }

            if (!AnyRelevant)
            {
                Popup.Show("That item has no cell or rechargeable capacitor in it.");
            }
            else
            if (!AnyRechargeable)
            {
                Popup.Show("That item cannot be recharged this way.");
            }
            else
            if (!AnyNotFullyCharged)
            {
                Popup.Show(GO.The + GO.DisplayNameOnly + GO.Is + " fully charged!");
            }
            if (AnyRecharged && Options.Sound)
            {
                SoundManager.PlaySound("whine_up");
            }
            return AnyRecharged;
        }

    }

}
