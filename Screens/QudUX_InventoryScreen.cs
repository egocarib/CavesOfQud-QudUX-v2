using System.Text;
using System.Globalization;
using System.Collections.Generic;
using Qud.API;
using ConsoleLib.Console;
using XRL.World;
using XRL.World.Parts;
using QudUX.ScreenExtenders;
using QudUX.Utilities;

namespace XRL.UI
{
    //This is based on the game code version of InventoryScreen.cs - there's some weirdness in here,
    //such as unused variables and unecessary code paths, labels, and interesting syntax choices that
    //I haven't edited in all cases. I've cleaned things up in a few places where I was making larger
    //adjustments, but the code is still a bit funky. I don't want to refactor it too much in case
    //further changes are made in the base game that cause me to return here and compare or update.
    [UIView("QudUX:Inventory", ForceFullscreen: true, NavCategory: "Charactersheet,Menu,Nocancelescape", UICanvas: null)]
    public class QudUX_InventoryScreen : IScreen, IWantsTextConsoleInit
    {
        static Dictionary<char, GameObject> SelectionList = new Dictionary<char, GameObject>();
        static Dictionary<char, QudUX_CategorySelectionListEntry> CategorySelectionList = new Dictionary<char, QudUX_CategorySelectionListEntry>();
        static Dictionary<string, List<GameObject>> CategoryMap = new Dictionary<string, List<GameObject>>();
        static Dictionary<string, QudUX_InventoryCategory> CategoryList = new Dictionary<string, QudUX_InventoryCategory>();
        static List<GameObject> SortList;
        static List<string> Categories = new List<string>();
        public static SortGODisplayName displayNameSorter = new SortGODisplayName();
        public static SortGOCategory categorySorter = new SortGOCategory();

        static int StartObject = 0;
        static int CategorySort = 0;
        static bool bMore = false;
        static QudUX_InventoryCategory forceCategorySelect = null;
        public static readonly int InventoryListHeight = 20;
        public static int ItemsSkippedByFilter = 0;
        public static int nSelected = 0;
        public static int currentMaxWeight;
        public static string FilterString = "";

        static TextConsole TextConsole;
        static ScreenBuffer Buffer;

        public void Init(TextConsole console, ScreenBuffer buffer)
        {
            TextConsole = console;
            Buffer = buffer;
        }

        public static void ClearLists()
        {
            CategoryMap.Clear();
            SelectionList.Clear();
            CategorySelectionList.Clear();
            CategoryList.Clear();
            SortList.Clear();
        }

        public static void ResetNameCache(GameObject GO)
        {
            Inventory pInventory = GO.GetPart("Inventory") as Inventory;
            List<GameObject> GOs = pInventory.GetObjectsDirect();
            for (int x = 0; x < GOs.Count; x++)
            {
                GOs[x].ResetNameCache();
            }
        }

        public static void RebuildLists( GameObject GO, InventoryScreenExtender.TabController TabController )
        {
            QudUX_InventoryScreenState SavedInventoryState = GO.RequirePart<QudUX_InventoryScreenState>();
            //TabController.RecalculateWeights(GO);
            Inventory pInventory = GO.GetPart("Inventory") as Inventory;
            CategoryMap.Clear();
            SelectionList.Clear();
            ItemsSkippedByFilter = 0;
            int listHeightMinusOne = InventoryListHeight - 1;
            bool bIsFiltered = (FilterString != "");

            if (!Categories.CleanContains("Category"))
            {
                Categories.Add("Category");
            }

            List<GameObject> Objs = pInventory.GetObjectsDirect();
            for (int x = 0; x < Objs.Count; x++)
            {
                GameObject Obj = Objs[x];
                if (!Obj.HasTag("HiddenInInventory"))
                {

                    string iCategory = Obj.GetInventoryCategory();
                    if (bIsFiltered && !Obj.GetCachedDisplayNameStripped().Contains(FilterString, CompareOptions.IgnoreCase))
                    {
                        ItemsSkippedByFilter++;
                        continue;
                    }
                    else if (!bIsFiltered)
                    {
                        if (TabController != null && !TabController.CurrentTabIncludes(iCategory))
                        {
                            //if we're not filtering by string, include only the categories associated with the current tab
                            continue;
                        }
                    }

                    Obj.Seen();

                    if (!CategoryList.ContainsKey(iCategory))
                    {
                        bool bExpandState = SavedInventoryState.GetExpandState(iCategory);
                        CategoryList.Add(iCategory, new QudUX_InventoryCategory(iCategory, bExpandState));
                        Categories.Add(iCategory);
                    }

                    if (!CategoryMap.ContainsKey(iCategory))
                    {
                        CategoryMap.Add(iCategory, new List<GameObject>());
                    }

                    CategoryMap[iCategory].Add(Obj);
                }
            }

            foreach (List<GameObject> MapList in CategoryMap.Values)
            {
                MapList.Sort(displayNameSorter);
            }

            while (CategorySort >= Categories.Count)
            {
                CategorySort--;
            }
            if (CategorySort == -1)
            {
                SortList = pInventory.GetObjects();
                SortList.Sort(displayNameSorter);
            }
            else
            if (Categories[CategorySort] == "Category")
            {
                SortList = pInventory.GetObjects();
                SortList.Sort(categorySorter);
            }
            else
            {
                if (CategoryMap.ContainsKey(Categories[CategorySort]))
                {
                    SortList = CategoryMap[Categories[CategorySort]];
                }
                SortList.Sort(displayNameSorter);
            }

            int nEntries = 0;
            bMore = false;
            if (CategorySort != -1 && Categories[CategorySort] == "Category")
            {
                CategorySelectionList.Clear();
                int nObject = 0;

                char c = 'a';

                List<string> CatNames = new List<string>();
                foreach (string N in CategoryList.Keys)
                {
                    CatNames.Add(N);
                }
                CatNames.Sort();

                for (int n = 0; n < CatNames.Count; n++)
                {
                    string sCat = CatNames[n];
                    QudUX_InventoryCategory Cat = CategoryList[sCat];

                    if (forceCategorySelect != null && Cat == forceCategorySelect)
                    {
                        if (nObject < StartObject)
                        {
                            StartObject = nObject;
                        }

                        nSelected = nObject - StartObject;
                        forceCategorySelect = null;
                    }

                    if (nObject >= StartObject && nObject <= listHeightMinusOne + StartObject)
                    {
                        CategorySelectionList.Add(c, new QudUX_CategorySelectionListEntry(Cat));
                        c++;
                        nEntries++;
                    }

                    if (Cat.Expanded && CategoryMap.ContainsKey(Cat.Name))
                    {
                        foreach (GameObject Obj in CategoryMap[Cat.Name])
                        {
                            nObject++;
                            nEntries++;

                            if (nObject >= StartObject && nObject <= listHeightMinusOne + StartObject)
                            {
                                CategorySelectionList.Add(c, new QudUX_CategorySelectionListEntry(Obj));
                                c++;
                            }
                            else
                            if (nObject > listHeightMinusOne + StartObject)
                            {
                                bMore = true;
                                break;
                            }
                        }
                    }

                    if (CategoryList.ContainsKey(sCat))
                    {
                        CategoryList[sCat].Weight = 0;
                        CategoryList[sCat].Items = 0;
                    }

                    if (CategoryMap.ContainsKey(Cat.Name))
                    {
                        foreach (GameObject Obj in CategoryMap[Cat.Name])
                        {
                            if (Obj.pPhysics != null)
                            {
                                CategoryList[sCat].Weight += Obj.pPhysics.Weight;
                            }
                            CategoryList[sCat].Items++;
                        }
                    }

                    if (nObject > listHeightMinusOne + StartObject)
                    {
                        bMore = true;
                        break;
                    }
                    nObject++;
                }
            }
            else
            {
                if (pInventory != null)
                {
                    int nObject = 0;

                    char c = 'a';

                    foreach (GameObject Obj in SortList)
                    {
                        if (nObject >= StartObject && nObject <= listHeightMinusOne + StartObject)
                        {
                            SelectionList.Add(c, Obj);
                            c++;
                        }
                        nObject++;

                        if (nObject > listHeightMinusOne + StartObject)
                        {
                            bMore = true;
                            break;
                        }
                    }
                }
            }

            List<string> RemovedCategories = new List<string>();
            foreach (string sCat in CategoryList.Keys)
            {
                if (!CategoryMap.ContainsKey(sCat))
                {
                    RemovedCategories.Add(sCat);
                }
                else
                if (CategoryMap[sCat].Count == 0)
                {
                    RemovedCategories.Add(sCat);
                }
            }

            foreach (string sRemovedCat in RemovedCategories)
            {
                if (CategoryList.ContainsKey(sRemovedCat))
                {
                    CategoryList.Remove(sRemovedCat);
                }
                if (CategoryMap.ContainsKey(sRemovedCat))
                {
                    CategoryMap.Remove(sRemovedCat);
                }
            }
        }




        public ScreenReturn Show(GameObject GO)
        {
            GameManager.Instance.PushGameView("QudUX:Inventory");
            QudUX_InventoryScreenState SavedInventoryState = GO.RequirePart<QudUX_InventoryScreenState>();
            InventoryScreenExtender.TabController TabController = new InventoryScreenExtender.TabController(GO);
            Inventory pInventory = GO.GetPart("Inventory") as Inventory;
            Body pBody = GO.GetPart("Body") as Body;
            ResetNameCache(GO);
            FilterString = "";
            Keys keys = 0;
            bool bDone = false;
            StartObject = 0;
            nSelected = 0;
            currentMaxWeight = Rules.Stats.GetMaxWeight(GO);
            bool AltDisplayMode = false;
            Dictionary<char, int> ItemMap = new Dictionary<char, int>();
            bool bShowInventoryTiles = QudUX.Concepts.Options.UI.ViewInventoryTiles;
            List<GameObject> disabledObjectsWithImposters = null;
            GameObject fakeTraderForPriceEval = GameObject.create("DromadTrader1");

            if (bShowInventoryTiles)
            {
                //temporarily disable unity prefab animations in the zone from coordinates 9,3 to 9,22 - this
                //is the area where we'll render tiles and where those animations would potentially animate
                //through the inventory screen.
                disabledObjectsWithImposters = ImposterUtilities.DisableImposters(GO.CurrentZone, 9, 3, 9, 22);
            }

            while (!bDone)
            {
                redraw:
                Event.ResetPool(resetMinEventPools: false);
                RebuildLists(GO, TabController);

                redrawnorebuild:

                Buffer.Clear();
                Buffer.SingleBox(0, 0, 79, 24, ColorUtility.MakeColor(TextColor.Grey, TextColor.Black));
                Buffer.SingleBox(0, 0, 79, 2, ColorUtility.MakeColor(TextColor.Grey, TextColor.Black));
                //Connect box intersections
                Buffer.Goto(0, 2);
                Buffer.Write(195);
                Buffer.Goto(79, 2);
                Buffer.Write(180);

                Buffer.Goto(35, 0);
                Buffer.Write("[ {{W|Inventory}} ]");

                Buffer.Goto(60, 0);
                Buffer.Write(" {{W|ESC}} or {{W|5}} to exit ");

                Buffer.Goto(50, 24);
                Buffer.Write("< {{W|7}} Character | Equipment {{W|9}} >");

                StringBuilder WeightString = Event.NewStringBuilder();
                WeightString
                    .Append("Total weight: {{Y|")
                    .Append(pInventory.GetWeight() + pBody.GetWeight())
                    .Append(" {{y|/}}  ")
                    .Append(Rules.Stats.GetMaxWeight(pInventory.ParentObject))
                    .Append(" lbs.}}")
                ;
                Buffer.Goto(79 - ColorUtility.LengthExceptFormatting(WeightString), 23);
                Buffer.Write(WeightString.ToString());

                ItemMap.Clear();

                int nObject = 0;

                QudUX_InventoryCategory CurrentCategory = null;
                GameObject CurrentObject = null;

                int yStart = 3;
                int xStart = 1;
                foreach (char keychar in CategorySelectionList.Keys)
                {
                    if (CategorySelectionList[keychar].Category != null) //category header
                    {
                        Buffer.Goto(xStart, yStart + nObject);
                        string nStart = "";

                        if (nObject == nSelected)
                        {
                            nStart = "{{Y|>}}";
                            CurrentCategory = CategorySelectionList[keychar].Category;
                        }
                        else
                        {
                            nStart = " ";
                        }

                        StringBuilder sWeight = Event.NewStringBuilder();
                        StringBuilder sCount = Event.NewStringBuilder();
                        char color = (nObject == nSelected) ? 'Y' : 'K';
                        sCount
                            .Append("{{")
                            .Append(color)
                            .Append('|')
                        ;
                        if (Options.ShowNumberOfItems)
                        {
                            sCount
                                .Append(", ")
                                .Append(CategorySelectionList[keychar].Category.Items)
                                .Append(CategorySelectionList[keychar].Category.Items == 1 ? " item" : " items")
                            ;
                        }
                        sCount.Append("}}");

                        sWeight
                            .Append(" {{")
                            .Append(nObject == nSelected ? 'Y' : 'y')
                            .Append("|[")
                            .Append(CategorySelectionList[keychar].Category.Weight)
                            .Append("#]}}")
                        ;

                        string expansionSymbol = CategorySelectionList[keychar].Category.Expanded ? "[-] " : "[+] ";

                        if (nObject == nSelected)
                        {
                            StringBuilder SB = Event.NewStringBuilder();
                            SB
                                .Append(nStart)
                                .Append(expansionSymbol)
                                .Append(keychar)
                                .Append(") {{K|[{{Y|")
                                .Append(CategorySelectionList[keychar].Category.Name)
                                .Append(sCount)
                                .Append("}}]}}")
                            ;
                            Buffer.Write(SB.ToString());
                        }
                        else
                        {
                            StringBuilder SB = Event.NewStringBuilder();
                            SB
                                .Append(nStart)
                                .Append(expansionSymbol)
                                .Append(keychar)
                                .Append(") {{K|[")
                                .Append(CategorySelectionList[keychar].Category.Name)
                                .Append(sCount)
                                .Append("]}}")
                            ;
                            Buffer.Write(SB.ToString());
                        }

                        Buffer.Goto(79 - ColorUtility.LengthExceptFormatting(sWeight), yStart + nObject);
                        Buffer.Write(sWeight);

                        ItemMap.Add(keychar, nObject);
                        nObject++;
                    }
                    else //item (not category header)
                    {
                        string nStart;
                        if (nObject == nSelected)
                        {
                            nStart = "{{Y|>}}    ";
                            CurrentObject = CategorySelectionList[keychar].Object;
                        }
                        else
                        {
                            nStart = "     ";
                        }

                        Buffer.Goto(xStart, yStart + nObject);
                        StringBuilder SB = Event.NewStringBuilder();
                        SB
                            .Append(nStart)
                            .Append(keychar)
                            .Append(") ")
                        ;
                        Buffer.Write(SB.ToString());

                        if (bShowInventoryTiles)
                        {
                            TileMaker objectTileInfo = new TileMaker(CategorySelectionList[keychar].Object);
                            objectTileInfo.WriteTileToBuffer(Buffer);
                            Buffer.X += 1;
                        }
                        Buffer.Write(CategorySelectionList[keychar].Object.DisplayName);

                        bool shouldHighlight = (nObject == nSelected);
                        StringBuilder detailString = Event.NewStringBuilder();
                        if (AltDisplayMode == false || QudUX.Concepts.Options.UI.ViewItemValues == false)
                        {
                            Physics pPhysics = CategorySelectionList[keychar].Object.pPhysics;
                            if (pPhysics != null)
                            {
                                int nWeight = pPhysics.Weight;
                                detailString.Append(" {{")
                                    .Append(shouldHighlight ? 'Y' : 'K')
                                    .Append("|")
                                    .Append(nWeight)
                                    .Append("#}}");
                            }
                        }
                        else
                        {
                            string valuePerPound = InventoryScreenExtender.GetItemValueString(CategorySelectionList[keychar].Object, fakeTraderForPriceEval, shouldHighlight);
                            detailString.Append(valuePerPound);
                        }
                        detailString.Append((char)179); //right box border segment in case item name overflowed the screen
                        Buffer.Goto(80 - ColorUtility.LengthExceptFormatting(detailString), yStart + nObject);
                        Buffer.Write(detailString);

                        ItemMap.Add(keychar, nObject);
                        nObject++;
                    }
                }

                if( nObject == 0 && StartObject != 0 )
                {
                    StartObject = 0;
                    goto redraw;
                }

                if (nSelected >= nObject)
                {
                    nSelected = nObject-1;
                    goto redraw;
                }

                if (FilterString != "")
                {
                    Buffer.Goto(3, 23);
                    Buffer.Write("{{y|" + ItemsSkippedByFilter + " items hidden by filter}}");
                    Buffer.Goto(1, 1);
                    Buffer.Write("{{y|Filtering on \"" + FilterString + "\" }}");
                    Buffer.Goto(58, 1);
                    Buffer.Write("{{y| {{W|DEL}} to remove filter}}");

                    if (CategorySelectionList.Count == 0)
                    {
                        Buffer.Goto(4, 5);
                        Buffer.Write("{{y|There are no matching items in your inventory.}}");
                    }
                }
                else
                {
                    Buffer.Goto(1, 1);
                    Buffer.Write(TabController.GetTabUIString());

                    if (CategorySelectionList.Count == 0)
                    {
                        if (TabController.CurrentTab != "Main" && TabController.CurrentTab != "Other")
                        {
                            Buffer.Goto(4, 5);
                            Buffer.Write("{{y|You are not carrying any " + TabController.CurrentTab.ToLower() + ".}}");
                        }
                    }

                    if (TabController.CurrentTab == "Main")
                    {
                        Buffer.Goto(2, 24);
                        Buffer.Write("{{y|{{W|Ctrl}}+{{W|M}} move category to Other}}");
                    }
                    else if (TabController.CurrentTab == "Other")
                    {
                        if (TabController.GetCategoriesForTab("Other").Count > 0)
                        {
                            Buffer.Goto(2, 24);
                            Buffer.Write("{{y|{{W|Ctrl}}+{{W|M}} move category to Main}}");
                        }
                        else
                        {
                            Buffer.Goto(4, 5);
                            Buffer.Write("{{y|There are no item categories here.}}");
                            Buffer.Goto(4, 7);
                            Buffer.Write("{{y|Select a category on the {{Y|Main}} tab and press {{W|Ctrl}}+{{W|M}} to move it here.}}");
                        }
                    }
                }

                Buffer.Goto(34, 24);
                Buffer.Write("{{y|[{{W|?}} view keys]}}");

                TextConsole.DrawBuffer(Buffer, ImposterManager.getImposterUpdateFrame()); //need to update imposters because we've toggled their visibility
                if (!XRL.Core.XRLCore.Core.Game.Running)
                {
                    if (bShowInventoryTiles)
                    {
                        ImposterUtilities.RestoreImposters(disabledObjectsWithImposters);
                    }
                    fakeTraderForPriceEval.Obliterate();
                    GameManager.Instance.PopGameView();
                    return ScreenReturn.Exit;
                }
                IEvent SentEvent = null;

                keys = ConsoleLib.Console.Keyboard.getvk(Options.MapDirectionsToKeypad, true);
                string ts = "";
                char ch = (ts + (char) Keyboard.Char + " ").ToLower()[0];
                if (keys == Keys.Enter)
                {
                    keys = Keys.Space;
                }

                if (keys == Keys.MouseEvent && Keyboard.CurrentMouseEvent.Event == "RightClick")
                {
                    bDone = true;
                }
                if (keys == Keys.OemQuestion || ch == '?')
                {
                    InventoryScreenExtender.HelpText.Show();
                }
                else
                if ((int) keys == 131137) // ctrl+a
                {
                    if (CurrentObject != null)
                    {
                        InventoryActionEvent.Check(out SentEvent, CurrentObject, GO, CurrentObject, "Eat");
                        ResetNameCache(GO);
                        ClearLists();
                    }
                }
                else
                if ((int)keys == 131140) // ctrl+d
                {
                    if (CurrentObject != null)
                    {
                        Event E = Event.New("CommandDropObject", "Object", CurrentObject);
                        SentEvent = E;
                        GO.FireEvent(E);
                        ResetNameCache(GO);
                        ClearLists();
                    }
                }
                else
                if ((int)keys == 131142 || ch == ',') // ctrl+f
                {
                    FilterString = Popup.AskString("Enter text to filter inventory by item name.", FilterString, 80, 0);
                    ClearLists();
                }
                else
                if (keys == Keys.Delete)
                {
                    FilterString = "";
                    ClearLists();
                }
                else
                if ((int)keys == 131154) // ctrl+r
                {
                    if (CurrentObject != null)
                    {
                        InventoryActionEvent.Check(out SentEvent, CurrentObject, GO, CurrentObject, "Drink");
                        ResetNameCache(GO);
                    }
                }
                else
                if ((int)keys == 131152) // ctrl+p
                {
                    if (CurrentObject != null)
                    {
                        InventoryActionEvent.Check(out SentEvent, CurrentObject, GO, CurrentObject, "Apply");
                        ResetNameCache(GO);
                        ClearLists();
                    }
                }
                else
                if (keys == Keys.NumPad7 || (keys == Keys.NumPad9 && Keyboard.RawCode != Keys.PageUp && Keyboard.RawCode != Keys.Next))
                {
                    bDone = true;
                }
                else
                if (keys == Keys.Escape || keys == Keys.NumPad5)
                {
                    bDone = true;
                }
                else
                if (keys == Keys.NumPad8)
                {
                    if (nSelected > 0)
                    {
                        nSelected--;
                        goto redrawnorebuild;
                    }
                    else
                    {
                        if( StartObject > 0 ) StartObject--;
                    }
                }
                else
                if (keys == Keys.NumPad2)
                {
                    if( nSelected < nObject-1 )
                    {
                        nSelected++;
                        goto redrawnorebuild;
                    }
                    else
                    {
                        if(bMore) StartObject++;
                    }
                }
                else
                if (keys == Keys.PageDown || keys == Keys.Next || Keyboard.RawCode == Keys.Next || Keyboard.RawCode == Keys.PageDown)
                {
                    if (nSelected < nObject-1)
                    {
                        nSelected = nObject-1;
                    }
                    else if (bMore)
                    {
                        StartObject += (InventoryListHeight - 1);
                    }
                }
                else
                if (keys == Keys.PageUp || keys == Keys.Back || Keyboard.RawCode == Keys.PageUp || Keyboard.RawCode == Keys.Back)
                {
                    if( nSelected > 0 )
                    {
                        nSelected = 0;
                    }
                    else
                    {
                        StartObject -= (InventoryListHeight - 1);
                        if (StartObject < 0) StartObject = 0;
                    }
                }
                else
                if (keys == Keys.Subtract || keys == Keys.OemMinus)
                {
                    foreach (QudUX_InventoryCategory Cat in CategoryList.Values)
                    {
                        Cat.Expanded = false;
                        SavedInventoryState.SetExpandState(Cat.Name, false);
                    }
                }
                else if (keys == Keys.Add || keys == Keys.Oemplus)
                {
                    foreach (QudUX_InventoryCategory Cat in CategoryList.Values)
                    {
                        Cat.Expanded = true;
                        SavedInventoryState.SetExpandState(Cat.Name, true);
                    }
                }
                else if (keys == Keys.Right || keys == Keys.NumPad6)
                {
                    TabController.Forward();
                    ClearLists();
                    StartObject = 0;
                    nSelected = 0;
                }
                else if (keys == Keys.Left || keys == Keys.NumPad4)
                {
                    TabController.Back();
                    ClearLists();
                    StartObject = 0;
                    nSelected = 0;
                }
                else if (keys == Keys.NumPad0 || keys == Keys.D0 || keys == Keys.OemPeriod || ch == '.')
                {
                    AltDisplayMode = !AltDisplayMode;
                }
                else if (keys == (Keys.Control | Keys.M))
                {
                    string iCategory = string.Empty;
                    if (CurrentObject != null)
                    {
                        foreach (var pair in CategoryMap)
                        {
                            if (pair.Value.Contains(CurrentObject))
                            {
                                iCategory = pair.Key;
                                break;
                            }
                        }
                    }
                    else if (CurrentCategory != null)
                    {
                        iCategory = CurrentCategory.Name;
                    }
                    if (!string.IsNullOrEmpty(iCategory))
                    {
                        if (TabController.CurrentTab == "Main")
                        {
                            string message = "{{y|Move the {{K|[{{Y|" + iCategory + "}}]}} category to the {{Y|Other}} tab?}}";
                            if (Popup.ShowYesNo(message) == DialogResult.Yes)
                            {
                                TabController.MoveCategoryFromMainToOther(iCategory);
                                ClearLists();
                            }
                        }
                        else if (TabController.CurrentTab == "Other")
                        {
                            string message = "{{y|Move the {{K|[{{Y|" + iCategory + "}}]}} category to the {{Y|Main}} tab?}}";
                            if (Popup.ShowYesNo(message) == DialogResult.Yes)
                            {
                                TabController.MoveCategoryFromOtherToMain(iCategory);
                                ClearLists();
                            }
                        }
                    }
                }
                else
                {
                    if (CurrentObject != null)
                    {
                        if (keys == (Keys.Control | Keys.E))
                        {
                            if (GO.AutoEquip(CurrentObject))
                            {
                                ResetNameCache(GO);
                            }
                        }

                        if (keys == Keys.NumPad1 || keys == Keys.D1 || keys == (Keys.Control | Keys.Left) || keys == (Keys.Control | Keys.NumPad4) || keys == (Keys.Control | Keys.Subtract) || keys == (Keys.Control | Keys.OemMinus))
                        {
                            //collapse the parent category for this item
                            foreach( var pair in CategoryMap )
                            {
                                if( pair.Value.Contains( CurrentObject ))
                                {
                                    CategoryList[pair.Key].Expanded = false;
                                    SavedInventoryState.SetExpandState(pair.Key, false);
                                    forceCategorySelect = CategoryList[pair.Key];
                                    break;
                                }
                            }
                        }

                        if (keys == Keys.Space)
                        {
                            Qud.API.EquipmentAPI.TwiddleObject(GO, CurrentObject, ref bDone);
                            ResetNameCache(GO);
                        }

                        if (keys == Keys.Tab)
                        {
                            InventoryActionEvent.Check(CurrentObject, GO, CurrentObject, "Look");
                            ResetNameCache(GO);
                        }
                    }

                    if (CurrentCategory != null)
                    {
                        if (keys == Keys.NumPad1 || keys == Keys.D1 || keys == (Keys.Control | Keys.Left) || keys == (Keys.Control | Keys.NumPad4) || keys == (Keys.Control | Keys.Subtract) || keys == (Keys.Control | Keys.OemMinus))
                        {
                            CurrentCategory.Expanded = false;
                            SavedInventoryState.SetExpandState(CurrentCategory.Name, false);
                        }

                        if (keys == Keys.NumPad3 || keys == Keys.D3 || keys == (Keys.Control | Keys.Right) || keys == (Keys.Control | Keys.NumPad6) || keys == (Keys.Control | Keys.Add) || keys == (Keys.Control | Keys.Oemplus))
                        {
                            CurrentCategory.Expanded = true;
                            SavedInventoryState.SetExpandState(CurrentCategory.Name, true);
                        }

                        if (keys == Keys.Space)
                        {
                            CurrentCategory.Expanded = !CurrentCategory.Expanded;
                            SavedInventoryState.ToggleExpandState(CurrentCategory.Name);
                        }
                    }

                    if (keys >= Keys.A && keys <= Keys.Z && CategorySelectionList.ContainsKey(ch))
                    {
                        if( nSelected == ItemMap[(char)ch] && (!CategorySelectionList.ContainsKey(ch) || CategorySelectionList[ch].Category == null))
                        {
                            EquipmentAPI.TwiddleObject(GO, CurrentObject, ref bDone);
                            ResetNameCache(GO);
                        }
                        else
                        {
                            nSelected = ItemMap[(char)ch];
                            if (CategorySelectionList.ContainsKey(ch) && CategorySelectionList[ch].Category != null)
                            {
                                CategorySelectionList[ch].Category.Expanded = !CategorySelectionList[ch].Category.Expanded;
                                SavedInventoryState.ToggleExpandState(CategorySelectionList[ch].Category.Name);
                            }
                        }
                    }
                }
                if (SentEvent != null && !bDone && SentEvent.InterfaceExitRequested())
                {
                    bDone = true;
                }
            }

            ClearLists();

            if (bShowInventoryTiles)
            {
                ImposterUtilities.RestoreImposters(disabledObjectsWithImposters);
            }
            fakeTraderForPriceEval.Obliterate();

            if (keys == Keys.NumPad7)
            {
                GameManager.Instance.PopGameView();
                return ScreenReturn.Previous;
            }
            if (keys == Keys.NumPad9)
            {
                GameManager.Instance.PopGameView();
                return ScreenReturn.Next;
            }
            GameManager.Instance.PopGameView();
            return ScreenReturn.Exit;
        }

    }

    /// <summary>
    /// Modeled after the base game's InventoryCategory class in 2.0.201 and earlier.
    /// </summary>
    public class QudUX_InventoryCategory
    {
        public string Name = "";

        public bool Expanded;

        public int Weight;

        public int Items;

        public QudUX_InventoryCategory(string Name, bool Expanded)
        {
            this.Name = Name;
            this.Expanded = Expanded;
        }
    }

    /// <summary>
    /// Modeled after the base game's CategorySelectionListEntry class, modified to accept QudUX_InventoryCategory
    /// </summary>
    public class QudUX_CategorySelectionListEntry
    {
        public QudUX_InventoryCategory Category;

        public GameObject Object;

        public QudUX_CategorySelectionListEntry(QudUX_InventoryCategory Cat)
        {
            Category = Cat;
        }

        public QudUX_CategorySelectionListEntry(GameObject GO)
        {
            Object = GO;
        }
    }
}
