using ConsoleLib.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using XRL.World;
using XRL.World.Parts;
using QudUX.Utilities;
using static QudUX.Utilities.Logger;

namespace XRL.UI
{
	[UIView("QudUX:CookIngredients", ForceFullscreen: true, NavCategory: "Menu,Nocancelescape", UICanvas: null)]
	public class QudUX_IngredientSelectionScreen : IScreen, IWantsTextConsoleInit
	{
		private static TextConsole Console;
		private static ScreenBuffer ScrapBuffer;

		public void Init(TextConsole console, ScreenBuffer buffer)
		{
			Console = console;
			ScrapBuffer = buffer;
		}

		/// <summary>
		/// Wrapper function for Show that is called from our Harmony patch
		/// </summary>
		public static int Static_Show(List<GameObject> ingredients, List<bool> isIngredientSelected)
        {
			Show(ingredients, isIngredientSelected);
			if (isIngredientSelected.Where(i => i == true).Count() > 0)
            {
				return 0; //ingredients were selected
            }
			return -1; //no ingredients selected, request interface exit.
		}

		/// <summary>
		/// Required implementation for the IScreen interface. This is never called directly.
		/// </summary>
		public ScreenReturn Show(GameObject subject)
        {
			return Show(null, null);
        }

		/// <summary>
		/// Class that holds basic state information for each ingredient option in the menu,
		/// and synchronizes option selections with the source boolean array we're patching.
		/// </summary>
		public class IngredientScreenInfo
		{
			public static List<bool> _SourceFlags;
			private int _IndexInSource;

			public IngredientScreenInfo(List<bool> source, int index)
			{
				_SourceFlags = source;
				_IndexInSource = index;
			}

			public string OptionName;
			public string CookEffect;
			public string UseCount;

			private static readonly string _CheckedBox = "{{y|[{{G|X}}]}} ";
			private static readonly string _UncheckedBox = "{{y|[ ]}} ";

			public string GetCheckboxString()
			{
				if (IsSelected)
				{
					return _CheckedBox + OptionName;
				}
				else
				{
					return _UncheckedBox + OptionName;
				}
			}

			public bool IsSelected
			{
				get { return _SourceFlags[_IndexInSource]; }
				set { _SourceFlags[_IndexInSource] = value; }
			}
		}

		/// <summary>
		/// Simplifies an ingredient's cook effect description by removing "Adds " from the front
		/// of it and removing " to cooked meals." from the end of it. As far as I know, all
		/// ingredient follow this descriptive structure. For example, "Adds regeneration and
		/// healing-based effects to cooked meals." becomes "Regeneration and healing-based effects"
		/// </summary>
		public static string SimplifyCookEffectDescription(string fullDescription, GameObject ingredient)
		{
			int endPos = fullDescription.IndexOf(" to cooked meals.");
			int startPos = fullDescription.IndexOf("Adds ");
			if (startPos < 0)
			{
				startPos = fullDescription.IndexOf("adds ");
			}
			if (endPos < 0 || startPos < 0)
			{
				LogUnique("(FYI) Unable to shorten cook effect description for item "
					+ $"'{ingredient?.DisplayNameStripped}' for display on IngredientSelectionScreen.");
				return fullDescription;
			}
			startPos += 5;
			int length = Math.Max(0, endPos - startPos);
			string result = fullDescription.Substring(startPos, length);
			result = result.Substring(0, 1).ToUpper() + result.Substring(1);
			return "{{rules|" + result + "}}";
		}

		/// <summary>
		/// Gets the player's total cookable drams on hand for each liquid type they are carrying.
		/// Adds together drams across multiple waterskins or similar containers.
		/// </summary>
		public static Dictionary<string, int> GetLiquidAmountsOnHand()
		{
			Dictionary<string, int> liquidAmountsOnHand = new Dictionary<string, int>();
			List<GameObject> validCookingIngredients = Campfire.GetValidCookingIngredients();
			foreach (GameObject thing in validCookingIngredients)
			{
				LiquidVolume lv = thing.LiquidVolume;
				if (lv != null)
				{
					string liquidName = lv.GetLiquidDescription(false);
					if (!liquidAmountsOnHand.ContainsKey(liquidName))
					{
						liquidAmountsOnHand[liquidName] = 0;
					}
					liquidAmountsOnHand[liquidName] += lv.Volume;
				}
			}
			return liquidAmountsOnHand;
		}

		/// <summary>
		/// Processes the list of ingredient game objects / boolean selections and creates a new
		/// array of IngredientScreenInfo data that is used for displaying those ingredients in our
		/// custom menu.
		/// </summary>
		public static List<IngredientScreenInfo> GetIngredientScreenInfo(List<GameObject> ingredients, List<bool> selections)
		{
			Dictionary<string, int> liquidAmountsOnHand = null;
			List<IngredientScreenInfo> ingredientInfo = new List<IngredientScreenInfo>();
			GetShortDescriptionEvent descriptionEvent = new GetShortDescriptionEvent();
			int index = 0;
			foreach (GameObject ing in ingredients)
			{
				IngredientScreenInfo info = new IngredientScreenInfo(selections, index);
				PreparedCookingIngredient ingPart = ing?.GetPart<PreparedCookingIngredient>();
				LiquidVolume liquid = ing?.LiquidVolume;
				string liquidDescription = string.Empty;

				//simple ingredient name
				if (ingPart != null)
				{
					info.OptionName = ing.ShortDisplayName;
				}
				else if (liquid != null)
				{
					liquidDescription = liquid.GetLiquidDescription(false);
					info.OptionName = ing.ShortDisplayName + " " + liquidDescription;
				}
				else
				{
					LogUnique("(Error) Unable to process ingredient description for ingredient "
						+ $"'{ing?.DisplayNameStripped} for display on IngredientSelectionScreen.");
				}

				//cook effect description
				string cookEffect = string.Empty;
				descriptionEvent.Postfix.Clear();
				if (ingPart != null)
				{
					ingPart.HandleEvent(descriptionEvent);
					cookEffect = descriptionEvent.Postfix.ToString();
				}
				else if (liquid != null)
				{
					liquid.HandleEvent(descriptionEvent);
					cookEffect = descriptionEvent.Postfix.ToString();
				}
				info.CookEffect = SimplifyCookEffectDescription(cookEffect, ing);

				//number of ingredient uses remaining
				int amount = 0;
				string unit = string.Empty;
				info.UseCount = string.Empty;
				if (ingPart != null)
				{
					Stacker stackInfo = ing.GetPart<Stacker>();
					amount = stackInfo != null ? stackInfo.Number : ingPart.charges;
					unit = "serving";
				}
				else if (liquid != null)
				{
					//Get the player's total volume of this type of liquid in all containers.
					//In the base game, the ingredient selector only shows the volume of the first
					//container found - it ignores additional drams in extra containers.
					if (liquidAmountsOnHand == null)
					{
						liquidAmountsOnHand = GetLiquidAmountsOnHand();
					}
					if (liquidAmountsOnHand.ContainsKey(liquidDescription))
					{
						amount = liquidAmountsOnHand[liquidDescription];
					}
					else
					{
						amount = liquid.Volume;
					}
					unit = "dram";
				}
				if (amount == 1)
				{
					info.UseCount = "{{y|  }}{{C|1}}{{y| " + unit + "}}";
				}
				else if (amount > 1 && amount <= 9)
				{
					info.UseCount = "{{y|  }}{{C|" + amount.ToString() + "}}{{y| " + unit + "s}}";
				}
				else if (amount > 1 && amount <= 99)
				{
					info.UseCount = "{{y| }}{{C|" + amount.ToString() + "}}{{y| " + unit + "s}}";
				}
				else if (amount >= 100)
				{
					info.UseCount = "{{C|" + amount.ToString() + "}}{{y| " + unit + "s}}";
				}

				ingredientInfo.Add(info);
				index++;
			}
			return ingredientInfo;
		}

		/// <summary>
		/// Our main menu screen method. Draws the ingredient selection screen and handles related functions.
		/// </summary>
		/// <remarks>
		/// This (and several supporting methods) are static just for simplicity's sake, because it
		/// is easier to call from a Harmony patch when it's static. If we were implementing this
		/// directly, we wouldn't make this static.
		/// </remarks>
		public static ScreenReturn Show(List<GameObject> ingredients, List<bool> isIngredientSelected)
		{
			if (ingredients == null || ingredients.Count <= 0 || isIngredientSelected == null || isIngredientSelected.Count != ingredients.Count)
            {
				return ScreenReturn.Exit;
			}

            GameManager.Instance.PushGameView("QudUX:CookIngredients");
			ScreenBuffer cachedScrapBuffer = ScreenBuffer.GetScrapBuffer2(true);
			Keys keys = Keys.None;
			bool shouldExitMenu = false;
			int scrollOffset = 0;
			int selectedIngredientIndex = 0;
			int scrollAreaHeight = 14;
			int amountColumnPos = 43;

			//Determine number of ingredients player is allowed to use
			int allowedIngredientCount = 2;
			if (IComponent<GameObject>.ThePlayer.HasSkill("CookingAndGathering_Spicer"))
			{
				allowedIngredientCount++;
			}

			//Gather ingredient display details
			List<IngredientScreenInfo> ingredientOptions = GetIngredientScreenInfo(ingredients, isIngredientSelected);

			while (!shouldExitMenu)
			{
				ScrapBuffer.Clear();
				Event.ResetPool();
				int selectedIngredientCount = ingredientOptions.Where(io => io.IsSelected == true).Count();

				//Draw main box
				ScrapBuffer.TitledBox("Ingredients");

				//Draw bottom box with selected ingredients
				ScrapBuffer.SingleBoxHorizontalDivider(17);
				int row = 18;
				foreach (IngredientScreenInfo info in ingredientOptions.Where(io => io.IsSelected == true))
				{
					ScrapBuffer.Write(2, row++, info.OptionName);
					ScrapBuffer.Write(5, row++, info.CookEffect);
					if (row > 23)
					{
						break;
					}
				}

				//Draw ingredient list
				if (ingredientOptions.Count == 0)
				{
					ScrapBuffer.Write(4, 3, "You don't have any ingredients.");
				}
				else
				{
					for (int drawIndex = scrollOffset; drawIndex < ingredientOptions.Count && drawIndex - scrollOffset < scrollAreaHeight; drawIndex++)
					{
						int yPos = 2 + (drawIndex - scrollOffset);
						ScrapBuffer.Goto(4, yPos);
						if (string.IsNullOrEmpty(ingredientOptions[drawIndex].OptionName))
						{
							ScrapBuffer.Write("&k<Error: unknown ingredient>&y");
						}
						else
						{
							string option = ingredientOptions[drawIndex].GetCheckboxString();
							ScrapBuffer.Write(option);
							if (ConsoleLib.Console.ColorUtility.LengthExceptFormatting(option) > 39)
							{
								ScrapBuffer.Write(40, yPos, "{{y|...             }}");
							}
						}

						ScrapBuffer.Goto(amountColumnPos, 2 + (drawIndex - scrollOffset));
						if (drawIndex == selectedIngredientIndex)
						{
							ScrapBuffer.Write("{{^K|" + ingredientOptions[drawIndex].UseCount + "}}");
						}
						else
						{
							ScrapBuffer.Write(ingredientOptions[drawIndex].UseCount);
						}

						//Draw selection caret at current index
						if (drawIndex == selectedIngredientIndex)
						{
							ScrapBuffer.Goto(2, 2 + (drawIndex - scrollOffset));
							ScrapBuffer.Write("{{Y|>}}");
						}
					}
				}

				//Help text on bottom of screen
				ScrapBuffer.Write(2, 24, " {{W|Space{{y|/}}Enter}}{{y| - Select}} ");
				ScrapBuffer.Write(46, 24, " {{W|C{{y|/}}Ctrl+Space{{y|/}}Ctrl+Enter}}{{y| - Cook}} ");

				//Infobox on right-hand side of screen
				ScrapBuffer.SingleBox(56, 0, 79, 17, ConsoleLib.Console.ColorUtility.MakeColor(TextColor.Grey, TextColor.Black));
				ScrapBuffer.Fill(57, 1, 78, 16, ' ', ConsoleLib.Console.ColorUtility.MakeColor(TextColor.Black, TextColor.Black));
				//Fix box intersections
				ScrapBuffer.Goto(56, 17);
				ScrapBuffer.Write(193);
				ScrapBuffer.Goto(56, 0);
				ScrapBuffer.Write(194);
				ScrapBuffer.Goto(79, 17);
				ScrapBuffer.Write(180);

				//Help text at top of screen
				ScrapBuffer.Write(2, 0, "{{y| {{W|2}} or {{W|8}} to scroll }}");
				ScrapBuffer.EscOr5ToExit();

				//Write description to infobox for currently highlighted ingredient
				string highlightDescription = ingredientOptions[selectedIngredientIndex].CookEffect;
				string[] linedHighlightDescription = StringFormat.ClipTextToArray(highlightDescription, 20).ToArray();
				ScrapBuffer.Goto(58, 2);
				ScrapBuffer.WriteBlockWithNewlines(linedHighlightDescription, 14);

				//Draw the screen
				Console.DrawBuffer(ScrapBuffer);

				//Respond to keyboard input
				keys = Keyboard.getvk(Options.MapDirectionsToKeypad);

				if (keys == Keys.Escape || keys == Keys.NumPad5)
				{
					//clear out the boolean list, since our current Harmony setup uses that as a method
					//to determine whether anything was selected from this menu
					for (int i = 0; i < isIngredientSelected.Count(); i++)
                    {
						isIngredientSelected[i] = false;
					}
					shouldExitMenu = true;
				}
				if (keys == Keys.NumPad8)
				{
					if (selectedIngredientIndex == scrollOffset)
					{
						if (scrollOffset > 0)
						{
							scrollOffset--;
							selectedIngredientIndex--;
						}
					}
					else if (selectedIngredientIndex > 0)
					{
						selectedIngredientIndex--;
					}
				}
				if (keys == Keys.NumPad2)
				{
					int maxIndex = ingredientOptions.Count - 1;
					if (selectedIngredientIndex < maxIndex)
					{
						selectedIngredientIndex++;
					}
					if (selectedIngredientIndex - scrollOffset >= scrollAreaHeight)
					{
						scrollOffset++;
					}
				}
				if (keys == Keys.Prior) //PgUp
				{
					selectedIngredientIndex = ((selectedIngredientIndex != scrollOffset) ? scrollOffset : (scrollOffset = Math.Max(scrollOffset - (scrollAreaHeight - 1), 0)));
				}
				if (keys == Keys.Next) //PgDn
				{
					if (selectedIngredientIndex != scrollOffset + (scrollAreaHeight - 1))
					{
						selectedIngredientIndex = scrollOffset + (scrollAreaHeight - 1);
					}
					else
					{
						int advancementDistance = scrollAreaHeight - 1;
						selectedIngredientIndex += advancementDistance;
						scrollOffset += advancementDistance;
					}
					selectedIngredientIndex = Math.Min(selectedIngredientIndex, ingredientOptions.Count - 1);
					scrollOffset = Math.Min(scrollOffset, ingredientOptions.Count - 1);
				}
				if (keys == Keys.C || keys == (Keys.Control | Keys.Enter) || keys == (Keys.Control | Keys.Space))
				{
					if (selectedIngredientCount > 0)
					{
						shouldExitMenu = true;
					}
					else
					{
						Popup.Show("You haven't selected any ingredients to cook with.", LogMessage: false);
					}
				}
				if (keys == Keys.Space || keys == Keys.Enter)
				{
					if (ingredientOptions[selectedIngredientIndex].IsSelected == true)
					{
						ingredientOptions[selectedIngredientIndex].IsSelected = false;
					}
					else
					{
						if (selectedIngredientCount >= allowedIngredientCount)
						{
							Popup.Show("You can't select more than " + selectedIngredientCount + " ingredients.", LogMessage: false);
						}
						else
						{
							ingredientOptions[selectedIngredientIndex].IsSelected = true;
						}
					}
				}
				if (keys == Keys.NumPad6)
				{
					if (ingredientOptions[selectedIngredientIndex].IsSelected == false)
					{
						if (selectedIngredientCount >= allowedIngredientCount)
						{
							Popup.Show("You can't select more than " + selectedIngredientCount + " ingredients.", LogMessage: false);
						}
						else
						{
							ingredientOptions[selectedIngredientIndex].IsSelected = true;
						}
					}
				}
				if (keys == Keys.NumPad4)
				{
					if (ingredientOptions[selectedIngredientIndex].IsSelected == true)
					{
						ingredientOptions[selectedIngredientIndex].IsSelected = false;
					}
				}
			}

			//Screen exit
			Console.DrawBuffer(cachedScrapBuffer);
			GameManager.Instance.PopGameView(true);
			return ScreenReturn.Exit;
		}
	}
}
