using System;
using System.Globalization;
using System.Collections.Generic;
using XRL.World;
using XRL.World.Parts;
using XRL.World.Parts.Mutation;
using ConsoleLib.Console;

namespace XRL.UI
{
	[UIView("QudUX:BuildLibrary", ForceFullscreen: true, NavCategory: "Menu,Nocancelescape", UICanvas: "QudUX:BuildLibrary")]
	public class QudUX_BuildLibraryScreen : IWantsTextConsoleInit
    {
		public static CharacterTemplate BuildTemplate;
		private static TextConsole Console;
		private static ScreenBuffer ScrapBuffer;

		public void Init(TextConsole console, ScreenBuffer buffer)
		{
			Console = console;
			ScrapBuffer = buffer;
		}

		public static string Show()
		{
			GameManager.Instance.PushGameView("QudUX:BuildLibrary");

			int currentIndex = 0;
			int scrollOffset = 0;
			int startX = 2;
			int startY = 2;
			int scrollHeight = 21;
			int infoboxStartX = 47;
			int infoboxStartY = 2;
			int infoboxEndY = 22;
			int infoboxHeight = infoboxEndY - infoboxStartY + 1;
			List<string> buildInfo = new List<string>();


			while (true)
			{
				int infoboxOffset = 0;
				string lastBuiltCode = "";
				buildInfo.Clear();

				BuildLibrary.Init();
				List<BuildEntry> buildEntries = BuildLibrary.BuildEntries;
				if (buildEntries == null)
				{
					buildEntries = new List<BuildEntry>();
				}

				if (currentIndex >= buildEntries.Count)
                {
					currentIndex = buildEntries.Count - 1;
                }
				if (scrollOffset < (currentIndex - scrollHeight + 1))
                {
					scrollOffset = currentIndex - scrollHeight + 1;
                }

				while (true)
				{
					Event.ResetPool();
					ScrapBuffer.Clear();
					ScrapBuffer.SingleBox(0, 0, 79, 24, ColorUtility.MakeColor(TextColor.Grey, TextColor.Black));
					ScrapBuffer.SingleBox(infoboxStartX - 2, 0, 79, 24, ColorUtility.MakeColor(TextColor.Grey, TextColor.Black));
					ScrapBuffer.Goto(32, 0);
					ScrapBuffer.Write("{{y|[ {{W|Build Library}} ]}}");
					ScrapBuffer.Goto(60, 0);
					ScrapBuffer.Write("{{y| {{W|ESC}} or {{W|5}} to exit }}");

					for (int i = scrollOffset; i < scrollOffset + scrollHeight && i < buildEntries.Count; i++)
					{
						int y = (i - scrollOffset) + startY;
						ScrapBuffer.Goto(startX, y);
						string prefix = (currentIndex == i) ? "{{Y|> }}{{W|" : "  {{w|";
						string postfix = "}}";
						int maxNameWidth = infoboxStartX - startX - 4;
						string buildName = buildEntries[i].Name;
						if (ColorUtility.StripFormatting(buildName).Length > maxNameWidth)
                        {
							buildName = ColorUtility.StripFormatting(buildName);
							buildName = buildName.Substring(0, maxNameWidth - 3) + "{{K|...}}";
                        }
						ScrapBuffer.Write($"{{{{y|{prefix}{buildName}{postfix}}}}}");
					}
					if (buildEntries.Count <= 0)
					{
						string[] array = StringFormat.ClipText("You don't have any character builds in your library. You can save a build after you create a character, or you can find builds online and import their codes.", 70).Split('\n');
						for (int j = 0; j < array.Length; j++)
						{
							ScrapBuffer.Goto(5, 4 + j);
							ScrapBuffer.Write(array[j]);
						}
					}

					string currentBuildCode = buildEntries[currentIndex].Code;
					if (currentBuildCode != lastBuiltCode)
                    {
						lastBuiltCode = currentBuildCode;
						MakeBody(currentBuildCode);
						buildInfo = GetBuildSidebarInfo();
					}

					bool hasScrollableBuildInfo = buildInfo.Count > infoboxHeight;
					for (int row = infoboxStartY; row <= infoboxEndY; row++)
					{
						int index = row - infoboxStartY + infoboxOffset;
						if (index >= buildInfo.Count)
                        {
							break;
                        }
						ScrapBuffer.Goto(infoboxStartX, row);
						ScrapBuffer.Write(buildInfo[index]);
                    }
					if (hasScrollableBuildInfo)
                    {
						if (buildInfo.Count > infoboxOffset + infoboxHeight)
						{
							ScrapBuffer.Goto(infoboxStartX, infoboxEndY);
							ScrapBuffer.Write("{{W|<More... {{y|use}} + {{y|to scroll down}}>}}");
						}
						if (infoboxOffset > 0)
                        {
							ScrapBuffer.Goto(infoboxStartX, infoboxStartY);
							ScrapBuffer.Write("{{W|<More... {{y|use}} - {{y|to scroll up}}>}}");
                        }
                    }

					ScrapBuffer.Goto(1, 24);
					ScrapBuffer.Write(" &WSpace&y-Select &WT&y-Tweet &WR&y-Rename &WC&y-Copy Code &WE&y-Enter Code &WP&y-Paste Code &WD&y-Delete ");

					Console.DrawBuffer(ScrapBuffer);

					Keys keys = Keyboard.getvk(Options.MapDirectionsToKeypad);

					if (keys == Keys.Escape || keys == Keys.NumPad5)
					{
						GameManager.Instance.PopGameView();
						return null;
					}
					if (keys == Keys.NumPad8)
					{
						if (currentIndex > 0)
						{
							currentIndex--;
							if (currentIndex < scrollOffset)
							{
								scrollOffset--;
							}
						}
						//break;
						continue;
					}
					if (keys == Keys.NumPad2)
					{
						if (currentIndex < buildEntries.Count - 1)
						{
							currentIndex++;
							if (currentIndex - scrollOffset >= scrollHeight)
							{
								scrollOffset++;
							}
						}
					}
					if (keys == Keys.E)
					{
						string code = Popup.AskString("Enter a character build code.", "", 60);
						if (!CreateCharacter.IsValidCode(code))
						{
							Popup.Show("That's an invalid code.");
							continue;
						}
						if (BuildLibrary.HasBuild(code))
						{
							Popup.Show("That character build is already in your library.");
							continue;
						}
						string name = Popup.AskString("Give this build a name.", "", 60);
						BuildLibrary.AddBuild(code, name);
						currentIndex = buildEntries.Count;
						break;
					}
					if (keys == Keys.Add || keys == Keys.Oemplus)
                    {
						if (hasScrollableBuildInfo)
						{
							if (buildInfo.Count > infoboxOffset + infoboxHeight)
							{
								infoboxOffset++;
							}
						}
					}
					if (keys == Keys.Subtract || keys == Keys.OemMinus)
                    {
						if (hasScrollableBuildInfo)
						{
							if (infoboxOffset > 0)
							{
								infoboxOffset--;
							}
						}
					}
					if (keys == Keys.P || keys == Keys.Insert || keys == Keys.V || keys == (Keys.Control | Keys.V))
					{
						string text = CreateCharacter.ClipboardHelper.GetClipboardData();
						if (!CreateCharacter.IsValidCode(text))
						{
							if (text == null)
							{
								text = "";
							}
							Popup.Show("The code you pasted is invalid.\n\n" + text);
							continue;
						}
						else if (BuildLibrary.HasBuild(text))
						{
							Popup.Show("That character build is already in your library.");
							continue;
						}
						else
						{
							string name2 = Popup.AskString("Give this build a name.", "", 60);
							BuildLibrary.AddBuild(text, name2);
							currentIndex = buildEntries.Count;
							break;
						}
					}
					if (keys == Keys.MouseEvent && Keyboard.CurrentMouseEvent.Event.StartsWith("Choice:"))
					{
						//mouse input not supported in QudUX version of build library
					}

					//remaining options require at least one entry
					if (buildEntries.Count <= 0)
					{
						continue;
					}

					if (keys == Keys.Enter || keys == Keys.Space)
					{
						if (!CreateCharacter.IsValidCode(currentBuildCode))
						{
							Popup.Show("This build's build code is no longer valid. It may be from an outdated version of the game.");
							continue;
						}
						MetricsManager.LogEvent("Chargen:Library:" + currentBuildCode);
						GameManager.Instance.PopGameView();
						return currentBuildCode;
					}
					if (keys == Keys.C || keys == (Keys.Control | Keys.C))
					{
						CreateCharacter.ClipboardHelper.SetClipboardData(currentBuildCode.ToUpper());
					}
					if (keys == Keys.R)
					{
						string text2 = Popup.AskString("Give this build a new name.", "", 60);
						if (!string.IsNullOrEmpty(text2))
						{
							buildEntries[currentIndex].Name = text2;
							BuildLibrary.UpdateBuild(buildEntries[currentIndex]);
							break;
						}
					}
					if (keys == Keys.T)
					{
						CreateCharacter.ShareToTwitter("HEY! Try my Caves of Qud character build. I call it, \""
							+ buildEntries[currentIndex].Name + "\".\n" + currentBuildCode.ToUpper());
					}
					if (keys == Keys.Delete || keys == Keys.D)
					{
						if (Popup.ShowYesNoCancel("Are you sure you want to delete this character build?") == DialogResult.Yes)
						{
							BuildLibrary.DeleteBuild(currentBuildCode);
							break;
						}
					}
				}
			}
		}

		public static List<string> GetBuildSidebarInfo()
        {
			List<string> buildInfo = new List<string>();
			buildInfo.Add("{{y|" + BuildTemplate.Genotype + ", " + BuildTemplate.Subtype + "}}");
			buildInfo.Add("");
			buildInfo.Add(MakeStatString(BuildTemplate.PlayerBody, "Strength"));
			buildInfo.Add(MakeStatString(BuildTemplate.PlayerBody, "Agility"));
			buildInfo.Add(MakeStatString(BuildTemplate.PlayerBody, "Toughness"));
			buildInfo.Add(MakeStatString(BuildTemplate.PlayerBody, "Intelligence"));
			buildInfo.Add(MakeStatString(BuildTemplate.PlayerBody, "Willpower"));
			buildInfo.Add(MakeStatString(BuildTemplate.PlayerBody, "Ego"));
			buildInfo.Add("");

			GameObjectBlueprint cyberneticBlueprint = GetCyberneticBlueprint();
			Mutations mutations = BuildTemplate.PlayerBody.GetPart<Mutations>();
			bool hasCybernetics = cyberneticBlueprint != null;
			bool hasMutations = mutations != null && mutations.MutationList.Count > 0;
			if (hasCybernetics)
			{
				buildInfo.Add("{{y|[ {{C|Cybernetics}} ]}}");
				buildInfo.Add("");
				buildInfo.Add(cyberneticBlueprint.GetPartParameter("Render", "DisplayName"));
				if (hasMutations)
				{
					buildInfo.Add("");
				}
			}
			if (hasMutations)
			{
				buildInfo.Add("{{y|[ {{M|Mutations}} ]}}");
				buildInfo.Add("");

				if (!string.IsNullOrEmpty(BuildTemplate.MutationLevel))
				{
					buildInfo.Add("{{C|" + BuildTemplate.MutationLevel + "}}");
				}
				for (int i = 0; i < mutations.MutationList.Count; i++)
				{
					string displayName = mutations.MutationList[i].DisplayName;
					if (!string.IsNullOrEmpty(displayName))
					{
						buildInfo.Add(displayName);
					}
					else
					{
						buildInfo.Add("{{R|*" + mutations.MutationList[i].Name + "*}}");
					}
				}
			}
			return buildInfo;
		}

		public static void MakeBody(string buildCode)
		{
			buildCode = buildCode.ToLower();
			BuildTemplate = new CharacterTemplate();

			string bodyObject = "Humanoid";
			string genotype = GetGenotype(buildCode);
			string subtype = GetSubtype(buildCode, genotype);
			if (!string.IsNullOrEmpty(genotype))
			{
				GenotypeEntry gEntry = GenotypeFactory.GenotypesByName[genotype];
				if (gEntry != null && !string.IsNullOrEmpty(gEntry.BodyObject))
				{
					bodyObject = GenotypeFactory.GenotypesByName[genotype].BodyObject;
				}
			}
			if (!string.IsNullOrEmpty(subtype))
			{
				SubtypeEntry sEntry = SubtypeFactory.SubtypesByName[subtype];
				if (sEntry != null && !string.IsNullOrEmpty(sEntry.BodyObject))
				{
					bodyObject = SubtypeFactory.SubtypesByName[subtype].BodyObject;
				}
			}
			BuildTemplate.Genotype = genotype;
			BuildTemplate.Subtype = subtype;
			BuildTemplate.BodyForm = bodyObject;
			BuildTemplate.PlayerBody = GameObject.create(bodyObject);
			BuildTemplate.PlayerStats = GameObject.create("Creature");


			foreach (Statistic value in BuildTemplate.PlayerStats.Statistics.Values)
			{
				BuildTemplate.PlayerBody.Statistics[value.Name] = new Statistic(value);
				BuildTemplate.PlayerBody.Statistics[value.Name].Owner = BuildTemplate.PlayerBody;
			}

			BuildTemplate.PlayerBody.Statistics["Strength"].BaseValue = buildCode[2] - 97 + 6;
			BuildTemplate.PlayerBody.Statistics["Agility"].BaseValue = buildCode[3] - 97 + 6;
			BuildTemplate.PlayerBody.Statistics["Toughness"].BaseValue = buildCode[4] - 97 + 6;
			BuildTemplate.PlayerBody.Statistics["Intelligence"].BaseValue = buildCode[5] - 97 + 6;
			BuildTemplate.PlayerBody.Statistics["Willpower"].BaseValue = buildCode[6] - 97 + 6;
			BuildTemplate.PlayerBody.Statistics["Ego"].BaseValue = buildCode[7] - 97 + 6;


			CreateCharacter.Template = BuildTemplate;
			CreateCharacter.InitCybernetics();

			ApplyCybernetics(BuildTemplate, buildCode);

			ValidateAttributes(BuildTemplate.PlayerBody);
			ApplyAttributeModifiers();

			List<MLNode> mutationNodes = GetMLNodes();
			SelectBuildMLNodes(buildCode, mutationNodes);
			ApplyMutations(BuildTemplate, mutationNodes);

			BuildTemplate.PlayerBody.Property.Add("MutationLevel", BuildTemplate.MutationLevel);
			BuildTemplate.PlayerBody.Property.Add("BodyForm", BuildTemplate.BodyForm);
			BuildTemplate.PlayerBody.Property.Add("Genotype", BuildTemplate.Genotype);
			BuildTemplate.PlayerBody.Property.Add("Subtype", BuildTemplate.Subtype);
			BuildTemplate.PlayerBody.Statistics["XP"].BaseValue = 0;
			BuildTemplate.PlayerBody.Statistics["Hitpoints"].BaseValue = BuildTemplate.PlayerBody.Stat("Toughness");

			ApplyCyberneticsStatMods();
		}

		public static string GetGenotype(string buildCode)
        {
			for (int i = 0; i < GenotypeFactory.Genotypes.Count; i++)
			{
				if ((buildCode[0].ToString() ?? string.Empty) == GenotypeFactory.Genotypes[i].Code)
				{
					return GenotypeFactory.Genotypes[i].Name;
				}
			}
			return string.Empty;
		}

		public static string GetSubtype(string buildCode, string genotype)
		{
			SubtypeClass subtypeClass = SubtypeFactory.ClassesByID[GenotypeFactory.GenotypesByName[genotype].Subtypes];
			List<SubtypeEntry> allSubtypes = subtypeClass.GetAllSubtypes();
			for (int i = 0; i < allSubtypes.Count; i++)
			{
				if (allSubtypes[i].Code == buildCode[1].ToString())
				{
					return allSubtypes[i].Name;
				}
			}
			return string.Empty;
		}

		private static void ApplyAttributeModifiers()
		{
			foreach (string key in BuildTemplate.subtypeEntry.Stats.Keys)
			{
				BuildTemplate.PlayerBody.Statistics[key].BaseValue += BuildTemplate.subtypeEntry.Stats[key].Bonus;
			}
			if (BuildTemplate.Props.ContainsKey("SelectedCyberneticBlueprint") && BuildTemplate.Props["SelectedCyberneticBlueprint"] == "00")
			{
				BuildTemplate.PlayerBody.Statistics["Toughness"].BaseValue++;
			}
		}

		private static void ValidateAttributes(GameObject GO)
		{
			int num = BuildTemplate.genotypeEntry.StatPoints;
			foreach (string key in BuildTemplate.genotypeEntry.Stats.Keys)
			{
				num += BuildTemplate.genotypeEntry.Stats[key].Minimum;
			}
			int num2 = 0;
			foreach (string key2 in BuildTemplate.genotypeEntry.Stats.Keys)
			{
				int baseValue = GO.Statistics[key2].BaseValue;
				num2 = ((baseValue <= 18) ? (num2 + baseValue) : (num2 + (18 + (baseValue - 18) * 2)));
			}
			if (num2 <= num)
			{
				return;
			}
			List<string> list = new List<string>(BuildTemplate.genotypeEntry.Stats.Keys);
			while (num2 > num)
			{
				list.Sort(delegate (string a, string b)
				{
					int num3 = GO.Statistics[b].BaseValue.CompareTo(GO.Statistics[a].BaseValue);
					return (num3 != 0) ? num3 : a.CompareTo(b);
				});
				int baseValue2 = GO.Statistics[list[0]].BaseValue;
				GO.Statistics[list[0]].BaseValue--;
				if (baseValue2 > 18)
				{
					num2--;
				}
				num2--;
			}
		}

		private static List<MLNode> GetMLNodes()
		{
			List<MLNode> nodes = new List<MLNode>();
			foreach (MutationCategory category in MutationFactory.GetCategories())
			{
				MLNode mLNode = new MLNode();
				mLNode.bExpand = false;
				mLNode.Category = category;
				nodes.Add(mLNode);
				foreach (MutationEntry entry in category.Entries)
				{
					MLNode mLNode2 = new MLNode();
					mLNode2.ParentNode = mLNode;
					mLNode2.Entry = entry;
					nodes.Add(mLNode2);
				}
			}
			return nodes;
		}

		private static void SelectBuildMLNodes(string buildCode, List<MLNode> mutationNodes)
        {
			string text = buildCode.Substring(8);
			for (int j = 0; j < text.Length; j += 2)
			{
				string text2 = text[j].ToString() + text[j + 1];
				foreach (MLNode node in mutationNodes)
				{
					switch (text2)
					{
						case "u1":
						case "u2":
						case "u3":
						case "u4":
						case "u5":
						case "u6":
							if (node.Entry != null && node.Entry.MutationCode == "uu")
							{
								if (text2 == "u1")
								{
									node.Selected = 1;
								}
								if (text2 == "u2")
								{
									node.Selected = 2;
								}
								if (text2 == "u3")
								{
									node.Selected = 3;
								}
								if (text2 == "u4")
								{
									node.Selected = 4;
								}
								if (text2 == "u5")
								{
									node.Selected = 5;
								}
								if (text2 == "u6")
								{
									node.Selected = 6;
								}
							}
							continue;
					}
					if (node.Entry != null && node.Entry.MutationCode == text2.Substring(0, 2))
					{
						node.Selected = 1;
						if ((node.Entry?.HasVariants() ?? false) && j < text.Length - 3 && text[j + 2] == '#')
						{
							j += 2;
							node.Variant = int.Parse(text[j + 1].ToString(), NumberStyles.HexNumber);
						}
					}
				}
			}
		}

		private static void ApplyMutations(CharacterTemplate buildTemplate, List<MLNode> mutationNodes)
		{
			if (!buildTemplate.genotypeEntry.IsMutant)
			{
				return;
			}
			Mutations part = buildTemplate.PlayerBody.GetPart<Mutations>();
			foreach (MLNode node in mutationNodes)
			{
				if (node.Entry != null && node.Selected > 0)
				{
					if (node.Entry.Mutation != null)
					{
						BaseMutation baseMutation = node.Entry.CreateInstance();
						baseMutation.SetVariant(node.Variant);
						part.AddMutation(baseMutation, node.Selected);
					}
					else if (node.Entry.DisplayName == "Chimera")
					{
						buildTemplate.MutationLevel = "Chimera";
					}
					else if (node.Entry.DisplayName == "Esper")
					{
						buildTemplate.MutationLevel = "Esper";
					}
				}
			}
		}

		private static void ApplyCybernetics(CharacterTemplate buildTemplate, string buildCode)
		{
			if (!buildTemplate.genotypeEntry.supportsCybernetics && !buildTemplate.subtypeEntry.supportsCybernetics)
            {
				return;
            }
			string value = buildCode.Substring(8, 2);
			int code = 0;
			if (!string.IsNullOrEmpty(value))
			{
				code = Convert.ToInt32(value);
			}
			if (code == 0)
			{
				buildTemplate.SetProp("SelectedCyberneticBlueprint", "00");
				buildTemplate.SetProp("SelectedCyberneticSlot", "00");
			}
			else
			{
				buildTemplate.SetProp("SelectedCyberneticBlueprint", CreateCharacter.cybernetics[code].Name);
				buildTemplate.SetProp("SelectedCyberneticSlot", CreateCharacter.cyberneticsBodypart[code]);
			}
		}

		private static GameObjectBlueprint GetCyberneticBlueprint()
		{
			if (BuildTemplate.Props.ContainsKey("SelectedCyberneticBlueprint") && BuildTemplate.Props["SelectedCyberneticBlueprint"] != "00")
			{
				return GameObjectFactory.Factory.Blueprints[BuildTemplate.Props["SelectedCyberneticBlueprint"]];
			}
			return null;
		}

		private static void ApplyCyberneticsStatMods()
        {
			GameObjectBlueprint gameObjectBlueprint = GetCyberneticBlueprint();
			if (gameObjectBlueprint != null)
			{
				string partParameter = gameObjectBlueprint.GetPartParameter("Cybernetics2StatModifier", "Stats");
				if (!string.IsNullOrEmpty(partParameter))
				{
					foreach (string item in partParameter.CachedCommaExpansion())
					{
						string[] array = item.Split(':');
						if (array.Length != 2)
						{
							continue;
						}
						try
						{
							string key = array[0];
							if (key != "Strength" && key != "Agility" && key != "Toughness" && key != "Intelligence" && key != "Willpower" && key != "Ego")
                            {
								continue;
                            }
							int num = Convert.ToInt32(array[1]);
							BuildTemplate.PlayerBody.Statistics[key].Bonus += num;
						}
						catch
						{
						}
					}
				}
			}
		}

		private static string MakeStatString(GameObject GO, string Stat)
        {
			string ret = "{{y|";
			ret += Stat.PadRight(15);
			int num = GO.Statistics[Stat].Value;
			int mod = GO.Statistics[Stat].Bonus;
			string statColor = (mod > 0) ? "G" : (mod < 0) ? "R" : "C";
			int statMod = XRL.Rules.Stat.GetScoreModifier(num);
			ret += "{{" + statColor + "|" + num + "}} ";
			ret += "{{c|(" + (statMod > 0 ? "+" : "") + statMod + ")}}";
			ret += "}}";
			return ret;
		}
	}
}
