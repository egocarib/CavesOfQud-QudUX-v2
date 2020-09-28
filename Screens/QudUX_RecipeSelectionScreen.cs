using ConsoleLib.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using XRL.Core;
using XRL.World;
using XRL.World.Parts;
using XRL.World.Skills.Cooking;
using QudUX.Utilities;

namespace XRL.UI
{
	[UIView("QudUX:CookRecipes", ForceFullscreen: true, NavCategory: "Menu,Nocancelescape", UICanvas: "QudUX:CookRecipes")]
	public class QudUX_RecipeSelectionScreen : IScreen, IWantsTextConsoleInit
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
		public static int Static_Show(List<Tuple<string, CookingRecipe>> recipeList)
        {
            Show(recipeList, out int result);
            return result;
        }

		/// <summary>
		/// Sorts recipes in the following order:
		///  * Recipes for which the player has all required ingredients
		///  * Recipes the player has favorited
		///  * Alphabetical order
		/// </summary>
		public static int RecipeComparator(Tuple<string, CookingRecipe> a, Tuple<string, CookingRecipe> b)
        {
			bool aHasIngredients = a.Item2.CheckIngredients();
			bool bHasIngredients = b.Item2.CheckIngredients();
			if (aHasIngredients && !bHasIngredients)
            {
				return -1;
            }
			if (bHasIngredients && !aHasIngredients)
            {
				return 1;
			}
			if (a.Item2.Favorite && !b.Item2.Favorite)
			{
				return -1;
			}
			if (b.Item2.Favorite && !a.Item2.Favorite)
			{
				return 1;
			}
			return string.Compare(
				ColorUtility.StripFormatting(a.Item2.GetDisplayName()),
				ColorUtility.StripFormatting(b.Item2.GetDisplayName())
			);
		}

		/// <summary>
		/// Required implementation for the IScreen interface. This is never called directly.
		/// </summary>
		public ScreenReturn Show(GameObject subject)
        {
			return Show(null);
        }

		/// <summary>
		/// Our main menu screen method. Draws the recipe selection screen and handles related functions.
		/// </summary>
		/// <remarks>
		/// This (and several supporting methods) are static just for simplicity's sake, because it
		/// is easier to call from a Harmony patch when it's static. If we were implementing this
		/// directly, we wouldn't make this static.
		/// </remarks>
		public static ScreenReturn Show(List<Tuple<string, CookingRecipe>> recipeList, out int screenResult)
		{
			screenResult = -1;
			if (recipeList == null || recipeList.Count <= 0 || recipeList.Any(r => r.Item2 == null))
			{
				return ScreenReturn.Exit;
			}

			recipeList.Sort(RecipeComparator);

			GameManager.Instance.PushGameView("QudUX:CookRecipes");
			ScreenBuffer cachedScrapBuffer = ScreenBuffer.GetScrapBuffer2(true);
			Keys keys = Keys.None;
			bool shouldExitMenu = false;
			int scrollOffset = 0;
			int scrollAreaHeight = 11;
			int selectedRecipeIndex = 0;

			while (!shouldExitMenu)
			{
				ScrapBuffer.Clear();
				Event.ResetPool();
				CookingRecipe selectedRecipe = recipeList[selectedRecipeIndex].Item2;

				//TODO: HANDLE 0 RECIPES (because user deleted them all)

				//Draw main box
				ScrapBuffer.TitledBox("Recipes");
				ScrapBuffer.SingleBoxHorizontalDivider(14);
				ScrapBuffer.EscOr5ToExit();
				//Help text
				ScrapBuffer.Write(2, 0, " {{W|2}} or {{W|8}} to scroll ");
				ScrapBuffer.Write(2, 24, " {{W|Space}}/{{W|Enter}} - Cook ");
				ScrapBuffer.Write(45, 24, " {{W|F}} - Favorite ");
				ScrapBuffer.Write(62, 24, " {{W|D}}/{{W|Del}} - Forget ");

				//Draw scrollable recipe list
				for (int drawIndex = scrollOffset; drawIndex < recipeList.Count && drawIndex - scrollOffset < scrollAreaHeight; drawIndex++)
				{
					CookingRecipe recipe = recipeList[drawIndex].Item2;
					string name = ColorUtility.StripFormatting(recipe.GetDisplayName());
					if (name.Length > 74)
                    {
						name = name.Substring(0, 71) + "...";
                    }
					name = (recipe.CheckIngredients() ? "{{W|" + name + "}}" : "{{K|" + name + "}}");
					name = (recipe.Favorite ? "{{R|\u0003" + name + "}}" : name);
					int xPos = recipe.Favorite ? 4 : 5;
					int yPos = 2 + (drawIndex - scrollOffset);
					
					ScrapBuffer.Write(xPos, yPos, name);
					
					if (drawIndex == selectedRecipeIndex)
					{
						ScrapBuffer.Write(2, yPos, "{{Y|>}}");
					}
				}

				//Draw ingredients
				int bottomPaneTextStartPos = 2;
				int maxBottomPaneWidth = 76;
				int ingredientPaneStartRow = 15;
				int ingredientPaneHeight = 2;
				ScrapBuffer.Goto(bottomPaneTextStartPos, ingredientPaneStartRow);
				List<string> ingredients = StringFormat.ClipTextToArray(selectedRecipe.GetBriefIngredientList(), maxBottomPaneWidth);
				for (int i = 0; i < ingredients.Count && i < ingredientPaneHeight; i++)
                {
					ScrapBuffer.WriteLine(ingredients[i]);
                }

				//Draw recipe effect description
				int recipeDescriptionPaneStartRow = 18;
				int recipeDescriptionPaneHeight = 6;
				List<string> description = StringFormat.ClipTextToArray(selectedRecipe.GetDescription(), maxBottomPaneWidth, KeepNewlines: true);
				if (description.Count > 6)
                {
					//This should be extremely rare - but if some single effects are longer than 2 lines for instance, this removes
					//line breaks to condense the effect description and ensure it can fit on 6 lines total.
					description = StringFormat.ClipTextToArray(selectedRecipe.GetDescription(), maxBottomPaneWidth, KeepNewlines: false);
				}
				ScrapBuffer.Goto(bottomPaneTextStartPos, recipeDescriptionPaneStartRow);
				for (int i = 0; i < description.Count && i < recipeDescriptionPaneHeight; i++)
				{
					ScrapBuffer.WriteLine(description[i]);
				}

				//Draw the screen
				Console.DrawBuffer(ScrapBuffer);

				//Respond to keyboard input
				keys = Keyboard.getvk(Options.MapDirectionsToKeypad);

				if (keys == Keys.Escape || keys == Keys.NumPad5)
				{
					screenResult = -1; //canceled
					shouldExitMenu = true;
				}
				if (keys == Keys.NumPad8)
				{
					if (selectedRecipeIndex == scrollOffset)
					{
						if (scrollOffset > 0)
						{
							scrollOffset--;
							selectedRecipeIndex--;
						}
					}
					else if (selectedRecipeIndex > 0)
					{
						selectedRecipeIndex--;
					}
				}
				if (keys == Keys.NumPad2)
				{
					int maxIndex = recipeList.Count - 1;
					if (selectedRecipeIndex < maxIndex)
					{
						selectedRecipeIndex++;
					}
					if (selectedRecipeIndex - scrollOffset >= scrollAreaHeight)
					{
						scrollOffset++;
					}
				}
				if (keys == Keys.Prior) //PgUp
				{
					selectedRecipeIndex = ((selectedRecipeIndex != scrollOffset) ? scrollOffset : (scrollOffset = Math.Max(scrollOffset - (scrollAreaHeight - 1), 0)));
				}
				if (keys == Keys.Next) //PgDn
				{
					if (selectedRecipeIndex != scrollOffset + (scrollAreaHeight - 1))
					{
						selectedRecipeIndex = scrollOffset + (scrollAreaHeight - 1);
					}
					else
					{
						int advancementDistance = scrollAreaHeight - 1;
						selectedRecipeIndex += advancementDistance;
						scrollOffset += advancementDistance;
					}
					selectedRecipeIndex = Math.Min(selectedRecipeIndex, recipeList.Count - 1);
					scrollOffset = Math.Min(scrollOffset, recipeList.Count - 1);
				}
				if (keys == Keys.F)
                {
					selectedRecipe.Favorite = !selectedRecipe.Favorite;
					recipeList.Sort(RecipeComparator);
					if (selectedRecipe.Favorite)
					{
						//if this was just marked as a favorite, update selection index to continue to point to it where it was moved
						for (int i = 0; i < recipeList.Count; i++)
						{
							if (recipeList[i].Item2 == selectedRecipe)
							{
								selectedRecipeIndex = i;
								//scroll the selection back into view if needed
								if (selectedRecipeIndex < scrollOffset || selectedRecipeIndex >= (scrollOffset + scrollAreaHeight))
								{
									scrollOffset = Math.Max(0, selectedRecipeIndex - (scrollAreaHeight / 2));
								}
							}
						}
					}
				}
				if (keys == Keys.D || keys == Keys.Delete)
				{
					if (Popup.ShowYesNo("{{y|Are you sure you want to forget your recipe for }}" + selectedRecipe.GetDisplayName() + "{{y|?}}") == DialogResult.Yes)
					{
						selectedRecipe.Hidden = true;
						for (int i = 0; i < recipeList.Count; i++)
                        {
							if (recipeList[i].Item2 == selectedRecipe)
                            {
								recipeList.RemoveAt(i);
								break;
                            }
						}
						selectedRecipeIndex = Math.Min(selectedRecipeIndex, recipeList.Count - 1);
						scrollOffset = Math.Min(scrollOffset, recipeList.Count - 1);
					}
				}
				if (keys == Keys.Space || keys == Keys.Enter)
				{
					if (!selectedRecipe.CheckIngredients())
					{
						List<ICookingRecipeComponent> missingComponents = selectedRecipe.Components.Where(c => !c.doesPlayerHaveEnough()).ToList();
						string message = "{{y|You don't have enough servings of }}";
						int idx = 0;
						foreach (ICookingRecipeComponent component in missingComponents)
                        {
							message += component.GetComponentSimpleName().PrependListElementJoinString(idx++, missingComponents.Count, "or");
                        }
						message += "{{y| to cook }}" + selectedRecipe.GetDisplayName() + "{{y|.}}";
						Popup.Show(message, LogMessage: false);
						continue;
					}
					else if (Popup.ShowYesNo("Cook " + selectedRecipe.GetDisplayName() + "&y?") == DialogResult.Yes)
                    {
						screenResult = selectedRecipeIndex;
						shouldExitMenu = true;
					}
				}
			}

			//Screen exit
			Console.DrawBuffer(cachedScrapBuffer);
			GameManager.Instance.PopGameView(true);
			return ScreenReturn.Exit;
		}
	}

	public static class QudUX_RecipeSelectionScreen_Extensions
	{
		/// <summary>
		/// A grammar extension of sorts. prepends a connector to a list element depending on it's position
		/// in the list, such as " and "  or  ", "  or  ", and " - uses commas only for 3 or more elements
		/// </summary>
		public static string PrependListElementJoinString(this string str, int zeroBasedElementIndex, int totalElements, string terminalConjunction = "and")
		{
			if (zeroBasedElementIndex == 0)
			{
				return str;
			}
			string joinString = string.Empty;
			if (totalElements > 2)
			{
				joinString += ",";
			}
			if (zeroBasedElementIndex == (totalElements - 1))
			{
				joinString += " " + terminalConjunction + " ";
			}
			else
			{
				joinString += " ";
			}
			return joinString + str;
		}

		/// <summary>
		/// Gets the amount associated with this cooking recipe component (i.e. ingredient). I believe
		/// this will always be 1 for cooking ingredients, but not 100% sure.
		/// </summary>
		public static int GetComponentAmount(this ICookingRecipeComponent component)
		{
			if (component is PreparedCookingRecipieComponentDomain componentDomain)
			{
				return componentDomain.amount;
			}
			if (component is PreparedCookingRecipieComponentLiquid componentLiquid)
			{
				return componentLiquid.amount;
			}
			if (component is PreparedCookingRecipieComponentBlueprint componentBlueprint)
			{
				return componentBlueprint.amount;
			}
			return 1;
		}

		/// <summary>
		/// Gets the display name of this cooking recipe component (i.e. ingredient), not including
		/// any units or additional phrases.
		/// </summary>
		public static string GetComponentSimpleName(this ICookingRecipeComponent component)
		{
			if (component is PreparedCookingRecipieComponentDomain componentDomain)
			{
				return componentDomain.ingredientType;
			}
			if (component is PreparedCookingRecipieComponentLiquid componentLiquid)
			{
				return LiquidVolume.getLiquid(componentLiquid.liquid).GetName();
			}
			if (component is PreparedCookingRecipieComponentBlueprint componentBlueprint)
			{
				return componentBlueprint.ingredientDisplayName;
			}
			return "{{K|<unknown ingredient>}}";
		}

		/// <summary>
		/// Gets the formatted string representing the list of ingredients required to cook this recipe.
		/// Shorter than the default game version of the ingredient list, which repeats "1 serving of "
		/// before every ingrient. Instead, this function returns a string similar to "1 serving each
		/// of <ingredient_one>, <ingredient_two>, and <ingredient_three>". This slightly shorter
		/// phrasing helps ensure the ingredient list can always fit on two lines in the menu.
		/// </summary>
		/// <remarks>
		///  * I am 99% sure a recipe can never call for an amount > 1 of any single ingredient.
		///    However, I've implemented logic below to account for this possiblity just to be safe.
		///  * This method defaults to using the "serving" language always, even for liquids. The
		///    default game uses "drams" but "serving" seems an appropriate adjective for liquids
		///    in the context of cooking a recipe, and allows us to use simpler phrasing herein.
		/// </remarks>
		public static string GetBriefIngredientList(this CookingRecipe recipe)
		{
			string ingredientList = string.Empty;
			int maxAmount = 0;
			List<string> ingredientNamesOnly = new List<string>(recipe.Components.Count);
			List<int> ingredientAmounts = new List<int>(recipe.Components.Count);
			for (int i = 0; i < recipe.Components.Count; i++)
			{
				ICookingRecipeComponent component = recipe.Components[i];
				ingredientAmounts.Add(component.GetComponentAmount());
				ingredientNamesOnly.Add(component.GetComponentSimpleName());
				if (ingredientAmounts[i] > maxAmount)
				{
					maxAmount = ingredientAmounts[i];
				}
			}
			if (maxAmount == 1)
			{
				ingredientList = "1 serving " + (recipe.Components.Count > 1 ? "each" : string.Empty) + " of ";
				for (int i = 0; i < recipe.Components.Count; i++)
				{
					ingredientList += ingredientNamesOnly[i].PrependListElementJoinString(i, recipe.Components.Count);
					ingredientList += "{{K|(" + CookingGamestate.GetIngredientQuantity(recipe.Components[i]) + ")}}";
				}
			}
			else
			{
				for (int i = 0; i < recipe.Components.Count; i++)
				{
					string thisIngredient = ingredientAmounts[i].ToString() + " serving" + (ingredientAmounts[i] > 1 ? "s" : string.Empty);
					thisIngredient += " of " + ingredientNamesOnly[i];
					ingredientList += (i > 0 ? ", " : string.Empty) + thisIngredient;
					ingredientList += "{{K|(" + CookingGamestate.GetIngredientQuantity(recipe.Components[i]) + ")}}";
				}
			}
			return ingredientList;
		}
	}
}
