using XRL.World;
using ConsoleLib.Console;
using QudUX.ScreenExtenders;
using System.Collections.Generic;
using QudUX.Utilities;
using System;

namespace XRL.UI
{
    [UIView("QudUX:CharacterTile", ForceFullscreen: true, NavCategory: "Menu,Nocancelescape", UICanvas: null)]
    public class QudUX_CharacterTileScreen : IScreen, IWantsTextConsoleInit
    {
        private static TextConsole Console;
        private static ScreenBuffer Buffer;
        private CharacterTemplate PlayerTemplate;
        private static bool HPColorWarningShown;

        public void Init(TextConsole console, ScreenBuffer buffer)
        {
            Console = console;
            Buffer = buffer;
        }

        public void Show(CharacterTemplate playerTemplate)
        {
            PlayerTemplate = playerTemplate;
            Show(PlayerTemplate.PlayerBody);
        }

        public enum ScreenMode
        {
            CoreTiles,
            ExtendedTilesMenu,
            ExtendedTiles
        }

        public ScreenReturn Show(GameObject targetBody)
        {
            ScreenMode screenMode = ScreenMode.CoreTiles;
            List<string> tileCategories = new List<string>()
            {
                "Animals", "Humanoids", "Robots", "Plants and Fungi", "Cherubim", "Statues", "Furniture", "Other (search)"
            };
            List<List<string>> tileCategoryBlueprintNodes = new List<List<string>>()
            {
                new List<string>() { "Animal", "BaseNest" },
                new List<string>() { "Humanoid" },
                new List<string>() { "Robot", "Baetyl" },
                new List<string>() { "Plant", "Fungus", "MutatedPlant", "MutatedFungus" },
                null,
                new List<string>() { "Statue", "Eater Hologram" },
                new List<string>() { "Furniture", "FoldingChair", "Vessel", "Catchbasin" },
                null
                //TODO: add "Pets" and "Sprites from Installed Mods" ?
            };
            int tileCategoryIndex = 0;
            GameManager.Instance.PushGameView("QudUX:CharacterTile");
            if (PlayerTemplate == null)
            {
                PlayerTemplate = new CharacterTemplate();
                PlayerTemplate.PlayerBody = targetBody;
            }
            CharacterTileScreenExtender characterTiler = new CharacterTileScreenExtender(PlayerTemplate);
            CharacterTileScreenExtender filterTiler = null;
            CharacterTileScreenExtender currentTiler = null;
            var blueprintTilers = new Dictionary<string, CharacterTileScreenExtender>();
            string filter = string.Empty;
            bool moreOptionSelected = false;
            bool shouldAskSearchQuery = false;

            while (true)
            {
                Event.ResetPool();
                Buffer.Clear();
                Buffer.TitledBox("Modify Character Sprite");
                Buffer.Write(2, 24, " {{W|Space}}/{{W|Enter}} Confirm selection ");

                if (screenMode == ScreenMode.CoreTiles)
                {
                    Buffer.EscOr5ToExit();
                    currentTiler = characterTiler;
                    Buffer.Goto(16, 9);
                    currentTiler.DrawTileLine(Buffer);
                    if (moreOptionSelected)
                    {
                        currentTiler.EraseSelectionBox(Buffer);
                        Buffer.Write(35, 14, "{{Y|>}} {{W|More...}}");
                    }
                    else
                    {
                        Buffer.Write(35, 14, "  More...");
                    }
                }

                else if (screenMode == ScreenMode.ExtendedTilesMenu)
                {
                    Buffer.EscOr5GoBack();
                    if (screenMode == ScreenMode.ExtendedTilesMenu)
                    {
                        Buffer.Write(21, 5, "Select a category of tiles to browse:");
                        Buffer.Goto(30, 8);
                        for (int i = 0; i < tileCategories.Count; i++)
                        {
                            string prefix = (i == tileCategoryIndex ? "{{Y|> }}{{W|" : "{{y|  ");
                            Buffer.WriteLine(prefix + tileCategories[i] + "}}");
                        }
                    }
                }

                else if (screenMode == ScreenMode.ExtendedTiles)
                {
                    Buffer.EscOr5GoBack();
                    Buffer.SingleBoxHorizontalDivider(3);
                    Buffer.SingleBoxHorizontalDivider(21);

                    string category = tileCategories[tileCategoryIndex];

                    if (category == "Other (search)")
                    {
                        Buffer.Write(49, 2, "{{W|,}} or {{W|Ctrl+F}} to change query");
                        if (!string.IsNullOrEmpty(filter))
                        {
                            if (filterTiler == null || filterTiler.CurrentQuery != filter)
                            {
                                Buffer.Write(35, 10, "Loading...");
                                Console.DrawBuffer(Buffer);
                                filterTiler = new CharacterTileScreenExtender(targetBody, filter);
                                Buffer.Write(35, 10, "          ");
                                filterTiler.ResetDrawArea(3, 5, 76, 19);
                            }
                            else
                            {
                                filterTiler.ResetDrawArea(3, 5, 76, 19, preserveSelection: true);
                            }
                            currentTiler = filterTiler;
                            currentTiler.DrawFillTiles(Buffer);
                            Buffer.Write(2, 2, "Filtering on: {{C|" + filter + "}}");
                            Buffer.Write(2, 22, currentTiler.SelectionDisplayName);
                            Buffer.Write(2, 23, "{{K|" + currentTiler.SelectionBlueprintPath + "}}");
                        }
                        else if (shouldAskSearchQuery)
                        {
                            shouldAskSearchQuery = false;
                            Console.DrawBuffer(Buffer);
                            UpdateSearchString(ref filter);
                            continue;
                        }
                    }
                    else //Preset tile category
                    {
                        CharacterTileScreenExtender tiler;
                        if (!blueprintTilers.TryGetValue(category, out tiler))
                        {
                            Buffer.Write(35, 10, "Loading...");
                            Console.DrawBuffer(Buffer);
                            if (tileCategoryBlueprintNodes[tileCategoryIndex] != null)
                            {
                                tiler = new CharacterTileScreenExtender(targetBody, tileCategoryBlueprintNodes[tileCategoryIndex]);
                            }
                            else
                            {
                                tiler = new CharacterTileScreenExtender(targetBody, category);
                            }
                            blueprintTilers.Add(category, tiler);
                            Buffer.Write(35, 10, "          ");
                        }
                        Buffer.Write(2, 2, "{{Y|" + category + "}}");
                        tiler.ResetDrawArea(3, 5, 76, 19, preserveSelection: (tiler == currentTiler));
                        tiler.DrawFillTiles(Buffer);
                        currentTiler = tiler;

                        Buffer.Write(2, 22, currentTiler.SelectionDisplayName);
                        Buffer.Write(2, 23, "{{K|" + currentTiler.SelectionBlueprintPath + "}}");
                    }

                }

                if ((screenMode == ScreenMode.CoreTiles && !moreOptionSelected) || screenMode == ScreenMode.ExtendedTiles)
                {
                    if (!currentTiler.IsPhotosynthetic)
                    {
                        Buffer.Write(39, 24, "<{{W|7}}/{{W|9}} Primary color>");
                    }
                    Buffer.Write(60, 24, "<{{W|+}}/{{W|-}} Detail color>");
                    if (screenMode == ScreenMode.ExtendedTiles)
                    {
                        Buffer.Write(70, 21, "<{{W|f}} Flip>");
                    }
                }

                Console.DrawBuffer(Buffer);

                Keys keys = Keyboard.getvk(Options.MapDirectionsToKeypad);
                char keyChar = Convert.ToChar(Keyboard.Char);

                if (keys == Keys.Escape || keys == Keys.NumPad5)
                {
                    if (screenMode == ScreenMode.CoreTiles)
                    {
                        GameManager.Instance.PopGameView();
                        return ScreenReturn.Exit;
                    }
                    else if (screenMode == ScreenMode.ExtendedTilesMenu)
                    {
                        screenMode = ScreenMode.CoreTiles;
                        moreOptionSelected = false;
                    }
                    else if (screenMode == ScreenMode.ExtendedTiles)
                    {
                        screenMode = ScreenMode.ExtendedTilesMenu;
                    }
                }
                if (keys == Keys.Enter || keys == Keys.Space)
                {
                    if (screenMode == ScreenMode.CoreTiles)
                    {
                        if (moreOptionSelected)
                        {
                            screenMode = ScreenMode.ExtendedTilesMenu;
                        }
                        else
                        {
                            currentTiler.ApplyToTargetBody();
                            GameManager.Instance.PopGameView();
                            return ScreenReturn.Exit;
                        }
                    }
                    else if (screenMode == ScreenMode.ExtendedTilesMenu)
                    {
                        screenMode = ScreenMode.ExtendedTiles;
                        filter = string.Empty;
                        shouldAskSearchQuery = true;
                    }
                    else if (screenMode == ScreenMode.ExtendedTiles)
                    {
                        if (currentTiler.CurrentTileForegroundColor().ToLower() != "y")
                        {
                            ShowHPOptionColorWarning();
                        }
                        currentTiler.ApplyToTargetBody();
                        GameManager.Instance.PopGameView();
                        return ScreenReturn.Exit;
                    }
                }
                if (keys == Keys.Add || keys == Keys.Oemplus)
                {
                    if ((screenMode == ScreenMode.CoreTiles && !moreOptionSelected) || screenMode == ScreenMode.ExtendedTiles)
                    {
                        currentTiler.RotateDetailColor(1);
                    }
                }
                if (keys == Keys.Subtract || keys == Keys.OemMinus)
                {
                    if ((screenMode == ScreenMode.CoreTiles && !moreOptionSelected) || screenMode == ScreenMode.ExtendedTiles)
                    {
                        currentTiler.RotateDetailColor(-1);
                    }
                }
                if (keys == Keys.NumPad2 || keys == Keys.NumPad4 || keys == Keys.NumPad6 || keys == Keys.NumPad8)
                {
                    if (screenMode == ScreenMode.ExtendedTiles || screenMode == ScreenMode.CoreTiles)
                    {
                        currentTiler.MoveSelection(keys);
                    }
                    if (keys == Keys.NumPad8)
                    {
                        if (screenMode == ScreenMode.ExtendedTilesMenu)
                        {
                            if (tileCategoryIndex > 0)
                            {
                                tileCategoryIndex--;
                            }
                        }
                        else if (screenMode == ScreenMode.CoreTiles && moreOptionSelected)
                        {
                            moreOptionSelected = false;
                        }
                    }
                    if (keys == Keys.NumPad2)
                    {
                        if (screenMode == ScreenMode.ExtendedTilesMenu)
                        {
                            if (tileCategoryIndex < tileCategories.Count - 1)
                            {
                                tileCategoryIndex++;
                            }
                        }
                        else if (screenMode == ScreenMode.CoreTiles && !moreOptionSelected)
                        {
                            moreOptionSelected = true;
                        }
                    }
                }
                if (keys == Keys.NumPad7 || keys == Keys.D7)
                {
                    if (screenMode == ScreenMode.ExtendedTiles || screenMode == ScreenMode.CoreTiles)
                    {
                        if (!currentTiler.IsPhotosynthetic)
                        {
                            ShowHPOptionColorWarning();
                            currentTiler.RotateForegroundColor(-1);
                        }
                        else
                        {
                            Popup.Show("You can't modify your primary color because you have Photosynthetic Skin.", LogMessage: false);
                        }
                    }
                }
                if (keys == Keys.NumPad9 || keys == Keys.D9)
                {
                    if (screenMode == ScreenMode.ExtendedTiles || screenMode == ScreenMode.CoreTiles)
                    {
                        if (!currentTiler.IsPhotosynthetic)
                        {
                            ShowHPOptionColorWarning();
                            currentTiler.RotateForegroundColor(1);
                        }
                        else
                        {
                            Popup.Show("You can't modify your primary color because you have Photosynthetic Skin.", LogMessage: false);
                        }
                    }
                }
                if (keys == Keys.F)
                {
                    currentTiler.Flip();
                }
                if (keys == Keys.Oemcomma || keyChar == ',' || keys == (Keys.Control | Keys.F))
                {
                    UpdateSearchString(ref filter);
                }
            }
        }

        private void ShowHPOptionColorWarning()
        {
            if (XRL.UI.Options.HPColor && !HPColorWarningShown)
            {
                string optionDescription = XRL.UI.Options.OptionsByID["Option@HPColor"].DisplayText;
                if (optionDescription.EndsWith("."))
                {
                    optionDescription = optionDescription.Remove(optionDescription.Length - 1);
                }
                Popup.Show("Note: You have the {{C|" + optionDescription + "}} option turned on, so "
                    + "the game will ignore any primary color chosen for your sprite.", LogMessage: false);
            }
            HPColorWarningShown = true;
        }

        private void UpdateSearchString(ref string filter)
        {
            string entry = Popup.AskString("Enter text to filter by object name.", "", 30);
            if (!string.IsNullOrEmpty(entry))
            {
                if (entry.Length >= 3)
                {
                    filter = entry;
                }
                else
                {
                    Popup.Show("You must enter at least three characters to perform a search.", LogMessage: false);
                }
            }
            return;
        }
    }
}
