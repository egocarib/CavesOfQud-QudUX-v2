using System;
using System.Linq;
using System.Collections.Generic;
using ConsoleLib.Console;
using XRL.World;
using XRL.World.Parts;
using QudUX.Utilities;

namespace XRL.UI
{
	[UIView("QudUX:AutogetManagement", ForceFullscreen: true, NavCategory: "Menu,Nocancelescape", UICanvas: null)]
	public class QudUX_AutogetManagementScreen : IScreen, IWantsTextConsoleInit
	{
		private static TextConsole Console;
		private static ScreenBuffer Buffer;

		public void Init(TextConsole console, ScreenBuffer buffer)
		{
			Console = console;
			Buffer = buffer;
		}

		public Dictionary<string, string> GetOptionList(NameValueBag AutogetSettings)
        {
			Dictionary<string, string> optionList = new Dictionary<string, string>();
			foreach (KeyValuePair<string, string> setting in AutogetSettings.Bag)
            {
				if (setting.Key.StartsWith("ShouldAutoget:") && setting.Value == "No")
                {
					var blueprint = GameObjectFactory.Factory.GetBlueprint(setting.Key.Split(':')[1]);
					string displayName = blueprint.DisplayName();
					if (string.IsNullOrEmpty(displayName))
                    {
						displayName = blueprint.Name;
                    }
					try { optionList.Add(displayName, setting.Key); }
					catch
                    {
						try { optionList.Add(blueprint.Name, setting.Key); }
						catch
                        {
							try { optionList.Add(displayName + " [" + blueprint.Name + "]", setting.Key); } catch { }
                        }
                    }
                }
			}
			return optionList;
        }

		public ScreenReturn Show(GameObject GO)
		{
			int scrollOffset = 0;
			int selectedIndex = 0;
			int scrollAreaHeight = 21;
			Dictionary<string, string> optionList = GetOptionList(QudUX_AutogetHelper.AutogetSettings);

			while (true)
			{
				List<string> optionStrings = optionList.Keys.ToList();

				Buffer.Clear();
				Buffer.SingleBox();
				Buffer.SingleBoxVerticalDivider(49);
				Buffer.Title("Auto-pickup Exclusions");
				Buffer.EscOr5ToExit();

				Buffer.Write(2, 24, " {{W|Space}}/{{W|Enter}}-Remove selected ");
				Buffer.Write(64, 24, " {{W|R}}-Remove all ");

				if (optionStrings.Count <= 0)
                {
					Buffer.Write(9, 2, "{{K|You haven't disabled auto-pickup}}");
					Buffer.Write(9, 3, "{{K|         for any items}}");
				}
				else
				{
					Buffer.Goto(2, 2);
					for (int i = scrollOffset; i < optionStrings.Count && i < (scrollOffset + scrollAreaHeight); i++)
					{
						string entry = (i == selectedIndex) ? "{{Y|> }}" : "  ";
						entry += optionStrings[i];
						Buffer.WriteLine(entry);
					}
				}

				Buffer.Fill(50, 1, 78, 23);
				Buffer.Goto(51, 2);
				Buffer.WriteLine("You've disabled auto-pickup");
				Buffer.WriteLine("for the items listed here.");
				Buffer.WriteLine("These exclusions apply to");
				Buffer.WriteLine("all characters.");
				Buffer.WriteLine("");
				Buffer.WriteLine("Your default game settings");
				Buffer.WriteLine("for auto-pickup will be");
				Buffer.WriteLine("applied first, and then any");
				Buffer.WriteLine("additional exclusions you");
				Buffer.WriteLine("have set here will also be");
				Buffer.WriteLine("applied");
				Buffer.WriteLine("");
				Buffer.WriteLine("You can add or remove ");
				Buffer.WriteLine("auto-pickup exclusions");
				Buffer.WriteLine("outside this menu by");
				Buffer.WriteLine("activating an item and");
				Buffer.WriteLine("selecting the option to");
				Buffer.WriteLine("\"disable auto-pickup\" or");
				Buffer.WriteLine("\"re-enable auto-pickup\"");

				Console.DrawBuffer(Buffer);

				Keys keys = Keyboard.getvk(Options.MapDirectionsToKeypad);

				if (keys == Keys.Escape || keys == Keys.NumPad5)
				{
					GameManager.Instance.PopGameView();
					return ScreenReturn.Exit;
				}
				if (keys == Keys.NumPad2)
                {
					if (selectedIndex < optionStrings.Count - 1)
                    {
						selectedIndex++;
						if (selectedIndex > (scrollOffset + scrollAreaHeight - 1))
                        {
							scrollOffset++;
                        }
                    }
                }
				if (keys == Keys.NumPad8)
                {
					if (selectedIndex > 0)
                    {
						selectedIndex--;
						if (selectedIndex < scrollOffset)
                        {
							scrollOffset = selectedIndex;
                        }
                    }
                }
				if ((keys == Keys.Space || keys == Keys.Enter) && optionStrings.Count > 0)
                {
					if (Popup.ShowYesNo($"Remove auto-pickup exclusion for {optionStrings[selectedIndex]}?") == DialogResult.Yes)
                    {
						string optionString = optionStrings[selectedIndex];
						string bagKey = optionList[optionString];
						optionList.Remove(optionString);
						QudUX_AutogetHelper.AutogetSettings.Bag.Remove(bagKey);
						QudUX_AutogetHelper.AutogetSettings.Flush();
						if (selectedIndex > optionList.Count - 1)
                        {
							selectedIndex = Math.Max(0, selectedIndex - 1);
                        }
					}
                }
				if (keys == Keys.R && optionStrings.Count > 0)
                {
					if (Popup.ShowYesNo("Remove ALL of your auto-pickup exclusions?") == DialogResult.Yes)
                    {
						string[] itemsToRemove = QudUX_AutogetHelper.AutogetSettings.Bag.Keys
							.Where(s => s.StartsWith("ShouldAutoget:")).ToArray();

						foreach (string item in itemsToRemove)
                        {
							QudUX_AutogetHelper.AutogetSettings.Bag.Remove(item);
						}
						QudUX_AutogetHelper.AutogetSettings.Flush();
						optionList = new Dictionary<string, string>();
					}
                }
			}
		}

	}
}
