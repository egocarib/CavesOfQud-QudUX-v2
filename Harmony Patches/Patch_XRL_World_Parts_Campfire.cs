using System;
using XRL.World;
using System.Linq;
using HarmonyLib;
using QudUX.Concepts;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using static QudUX.Concepts.Constants.MethodsAndFields;
using static QudUX.HarmonyPatches.PatchHelpers;

namespace QudUX.HarmonyPatches
{
    /// <summary>
    /// 
    /// Egocarib's version was only patching Campfire.Cook but everything has
    /// been reworked. I kept his work for prosperity, and patched the 
    /// new relevant methods:
    /// • Campfire.CookFromIngredients
    /// • Campfire.CookFromRecipe
    /// 
    /// </summary>
    [HarmonyPatch(typeof(XRL.World.Parts.Campfire))]
    public class Patch_XRL_World_Parts_Campfire_Cooking
    {

        [HarmonyPrepare]
        static bool Prepare()
        {
            if (!Options.UI.UseQudUXCookMenus)
            {
                PatchHelpers.LogPatchResult("Campfire",
                    "Skipped. The \"Use revamped cooking menus\" option is disabled.");
                return false;
            }
            return true;
        }

        #region OLD_PATCH
        /// <summary>
    /// The following patch replaces the "cook with ingredients" and "choose recipe" selection screens
    /// with entirely new UI screens. This is a fairly complex transpiler that modifies a number of
    /// details in the Campfire class. Unfortunately the code that handles these menus is overly
    /// complex, as it uses a lot of looping pop-ups instead of real menus.
    /// </summary>

        // This is where we find the first array parameter that we are looking for (ingredient GameObject array)
        private readonly static CodeInstruction FirstSegmentTargetInstruction = new CodeInstruction(OpCodes.Call, Campfire_GetValidCookingIngredients);

        // This is where we find the second array parameter that we are looking for (corresponding "selected ingredient" boolean array)
        private readonly static List<CodeInstruction> SecondSegmentTargetInstructions = new List<CodeInstruction>()
        {
            new CodeInstruction(OpCodes.Ldstr, "CookingAndGathering_Spicer"),
            new CodeInstruction(OpCodes.Callvirt, GameObject_HasSkill),
            new CodeInstruction(OpCodes.Newobj, List_Bool_ctor)
        };

        // This brings us to just before parameters are pushed onto the stack for the "choose ingredients" method we want to replace
        private readonly static List<CodeInstruction> ThirdSegmentTargetInstructions = new List<CodeInstruction>()
        {
            new CodeInstruction(OpCodes.Endfinally),
            new CodeInstruction(OpCodes.Ldstr, "Choose ingredients to cook with.")
        };

        // This is the "choose ingredients" method we want to replace
        private readonly static CodeInstruction ThirdSegmentFinalInstruction = new CodeInstruction(OpCodes.Call, Popup_ShowOptionList);

        // Here is where we modify a flag to force recipes with missing ingredients to be included in the array
        private readonly static CodeInstruction FourthSegmentTarget1_Instruction = new CodeInstruction(OpCodes.Ldc_I4_2);
        private readonly static OpCode FourthSegmentTarget2_OpCodeOnly = OpCodes.Bne_Un;
        private readonly static CodeInstruction FourthSegmentTarget3_Instruction = new CodeInstruction(OpCodes.Ldc_I4_0);
        private readonly static int FourthSegmentAllowedInstructionDistance = 4;

        // This brings us to just before parameters are pushed onto the stack for the "choose recipe" method we want to replace
        private readonly static CodeInstruction FifthSegmentTargetInstruction = new CodeInstruction(OpCodes.Ldstr, "Choose a recipe");
        private readonly static CodeInstruction FifthSegmentFinalInstruction = new CodeInstruction(OpCodes.Call, Popup_ShowOptionList);

        // Here is where we null out (Nop) some instructions that we no longer need because our new menu replaces these functionalities
        private readonly static CodeInstruction SixthSegmentTargetInstruction = new CodeInstruction(OpCodes.Ldstr, "Add to favorite recipes");
        private readonly static CodeInstruction SixthSegmentFinalInstruction = new CodeInstruction(OpCodes.Newobj, List_GameObject_ctor);

        //max allowed distance between individual instructions in the above sequences
        private static int AllowedInstructionDistance = 20;

        // [HarmonyTranspiler]
        // [HarmonyPatch("Cook")]
        static IEnumerable<CodeInstruction> OldCampfirePatch(IEnumerable<CodeInstruction> instructions)
        {
            int patchSegment = 1;
            int patchSegment1_stloc_ct = 0;
            object ingredientList_LocalVarIndex = null;
            object ingredientBools_LocalVarIndex = null;
            object recipeList_LocalVarIndex = null;
            int idx = 0;
            int gap = 0;
            bool found = false;
            bool patchComplete = false;
            foreach (var instruction in instructions)
            {
                if (found)
                {
                    if (patchSegment == 1)
                    {
                        if (instruction.opcode == OpCodes.Stloc_S)
                        {
                            patchSegment1_stloc_ct++;
                        }
                        if (patchSegment1_stloc_ct == 2)
                        {
                            //save the local variable index of the ingredient list
                            ingredientList_LocalVarIndex = instruction.operand;
                            patchSegment++;
                            found = false;
                        }
                    }
                    else if (patchSegment == 2)
                    {
                        if (instruction.opcode == OpCodes.Stloc_S)
                        {
                            ingredientBools_LocalVarIndex = instruction.operand;
                            patchSegment++;
                            found = false;
                        }
                    }
                    else if (patchSegment == 3)
                    {
                        //ignore all the lines that push stuff onto the stack for Popup.ShowOptionList
                        if (!PatchHelpers.InstructionsAreEqual(instruction, ThirdSegmentFinalInstruction))
                        {
                            continue;
                        }
                        //replace the call to Popup.ShowOptionList with our custom ingredient selection menu
                        yield return new CodeInstruction(OpCodes.Ldloc_S, ingredientList_LocalVarIndex);
                        yield return new CodeInstruction(OpCodes.Ldloc_S, ingredientBools_LocalVarIndex);
                        yield return new CodeInstruction(OpCodes.Call, QudUX_IngredientSelectionScreen_Static_Show);
                        patchSegment++;
                        found = false;
                        continue;
                    }
                    else if (patchSegment == 4)
                    {
                        //unused
                    }
                    else if (patchSegment == 5)
                    {
                        if (recipeList_LocalVarIndex == null && instruction.opcode == OpCodes.Ldloc_S)
                        {
                            //grab the recipe list variable, we'll need it below
                            recipeList_LocalVarIndex = instruction.operand;
                        }
                        else if (PatchHelpers.InstructionsAreEqual(instruction, FifthSegmentFinalInstruction))
                        {
                            //replace the call to Popup.ShowOptionList with our custom recipe selection menu
                            yield return new CodeInstruction(OpCodes.Ldloc_S, recipeList_LocalVarIndex);
                            yield return new CodeInstruction(OpCodes.Call, QudUX_RecipeSelectionScreen_Static_Show);
                            patchSegment++;
                            found = false;
                        }
                        continue;
                    }
                    else if (!patchComplete)
                    {
                        if (PatchHelpers.InstructionsAreEqual(instruction, SixthSegmentFinalInstruction))
                        {
                            patchComplete = true;
                            PatchHelpers.LogPatchResult("Campfire",
                                "Patched successfully." /* Adds completely new UI screens for ingredient- and recipe-based cooking. */ );
                            //allow this instruction to fall through, we want it and everything after it.
                        }
                        else
                        {
                            //null out various instructions (several of them are used as labels, so can't just ignore them)
                            instruction.opcode = OpCodes.Nop;
                            instruction.operand = null;
                        }
                    }
                }
                else if (patchSegment == 1)
                {
                    if (PatchHelpers.InstructionsAreEqual(instruction, FirstSegmentTargetInstruction))
                    {
                        found = true;
                        idx = 0;
                    }
                }
                else if (patchSegment == 2)
                {
                    if (PatchHelpers.InstructionsAreEqual(instruction, SecondSegmentTargetInstructions[idx]))
                    {
                        if (++idx == SecondSegmentTargetInstructions.Count())
                        {
                            found = true;
                            idx = 0;
                        }
                        gap = 0;
                    }
                    else
                    {
                        if (++gap > AllowedInstructionDistance)
                        {
                            idx = 0;
                        }
                    }
                }
                else if (patchSegment == 3)
                {
                    if (PatchHelpers.InstructionsAreEqual(instruction, ThirdSegmentTargetInstructions[idx]))
                    {
                        if (++idx == ThirdSegmentTargetInstructions.Count())
                        {
                            found = true;
                            instruction.opcode = OpCodes.Nop; //null out this instruction (can't remove it because it's used as a label)
                            instruction.operand = null;
                            idx = 0;
                        }
                        gap = 0;
                    }
                    else
                    {
                        if (++gap > AllowedInstructionDistance)
                        {
                            idx = 0;
                        }
                    }
                }
                else if (patchSegment == 4)
                {
                    if (idx == 0)
                    {
                        if (PatchHelpers.InstructionsAreEqual(instruction, FourthSegmentTarget1_Instruction))
                        {
                            idx++;
                        }
                    }
                    else if (idx == 1)
                    {
                        if (instruction.opcode == FourthSegmentTarget2_OpCodeOnly)
                        {
                            idx++;
                        }
                        else
                        {
                            idx = 0;
                        }
                    }
                    else if (idx == 2)
                    {
                        if (!PatchHelpers.InstructionsAreEqual(instruction, FourthSegmentTarget3_Instruction))
                        {
                            if (++gap > FourthSegmentAllowedInstructionDistance)
                            {
                                idx = 0;
                                gap = 0;
                            }
                        }
                        else
                        {
                            instruction.opcode = OpCodes.Ldc_I4_1; //modify to set flag to true instead of false, so that recipes without ingredients on hand aren't hidden from the array
                            patchSegment++;
                            idx = 0;
                            gap = 0;
                        }
                    }
                }
                else if (patchSegment == 5)
                {
                    if (PatchHelpers.InstructionsAreEqual(instruction, FifthSegmentTargetInstruction))
                    {
                        found = true;
                        instruction.opcode = OpCodes.Nop; //null out this instruction (can't remove it because it's used as a label)
                        instruction.operand = null;
                    }
                }
                else if (patchSegment == 6)
                {
                    if (PatchHelpers.InstructionsAreEqual(instruction, SixthSegmentTargetInstruction))
                    {
                        found = true;
                        instruction.opcode = OpCodes.Nop;
                        instruction.operand = null;
                    }
                }
                yield return instruction;
            }
            if (patchComplete == false)
            {
                PatchHelpers.LogPatchResult("Campfire",
                    "Failed. This patch may not be compatible with the current game version. "
                    + "The game's default cooking UI pop-ups will be used instead of QudUX's revamped screens.");
            }
        }
#endregion
        

        [HarmonyTranspiler]
        [HarmonyPatch("CookFromIngredients")]
        static IEnumerable<CodeInstruction> Transpiler_CookFromIngredient(IEnumerable<CodeInstruction> instructions, ILGenerator gen)
        {
            //First we need to retrieve Tuple List that compiles all ingredients data
            //Then we need to retrieve the list of bool representing selected ingredients
            //Then we jump over the section that is displaying vanilla selection menu
            //and finaly we push our custom screen

            // Ingredients are stored in a List<Tuple<int, GameObject, string>>, saved on index 3
            // It is accessible through OPCode Ldloc_3, so we don't need to look for its dedicated
            // index. We can directly look for the list of bool
            var Seq1 = new PatchTargetInstructionSet( new List<PatchTargetInstruction>(){
                new PatchTargetInstruction(OpCodes.Ldloca_S),
                new PatchTargetInstruction(OpCodes.Call, 0),
                new PatchTargetInstruction(OpCodes.Pop, 0),
                new PatchTargetInstruction(OpCodes.Ldloc_S, 0), // push List<bool> on stack
                new PatchTargetInstruction(OpCodes.Ldc_I4_0, 0),
                new PatchTargetInstruction(OpCodes.Callvirt, 0),
                new PatchTargetInstruction(OpCodes.Ldloca_S, 0),
                new PatchTargetInstruction(OpCodes.Call, 0),
                new PatchTargetInstruction(OpCodes.Brtrue_S, 0),
                new PatchTargetInstruction(OpCodes.Leave_S, 0)
            });
            
            // Seq2 is used to detect the end of the section displaying vanilla selection menu
            // It stops exactly before the loop that will evaluate selection List
            // Which is right were we want our menu to be displayed
            var Seq2 = new PatchTargetInstructionSet( new List<PatchTargetInstruction>()
            {
                new PatchTargetInstruction(OpCodes.Ldloc_S),
                new PatchTargetInstruction(OpCodes.Ldc_I4_1, 0),
                new PatchTargetInstruction(OpCodes.Sub, 0),
                new PatchTargetInstruction(OpCodes.Stloc_S, 0),
                new PatchTargetInstruction(OpCodes.Br, 0),
                new PatchTargetInstruction(OpCodes.Ldloc_S, 0),
                new PatchTargetInstruction(OpCodes.Brtrue_S, 0),
                new PatchTargetInstruction(OpCodes.Ldloc_S, 0),
                new PatchTargetInstruction(OpCodes.Ldloc_S, 0),
                new PatchTargetInstruction(OpCodes.Bgt, 0),
                new PatchTargetInstruction(OpCodes.Ldc_I4_0, 0) 
            });

            int seq=1;
            bool patched = false;
            CodeInstruction seq2Last = null;
            Label jumpOverSourceDisplay = gen.DefineLabel();

            foreach(CodeInstruction instruction in instructions)
            {
                // When completed, we do nothing.
                // The point of this sequence is to:
                // • Get the instruction that pushes the list<bool> of selected ingredients
                // • Get the instruction right before the scope display source menu to setup a jump over it
                if(seq == 1 && Seq1.IsMatchComplete(instruction))
                {
                    // instruction below is leave.s, originally jumping right into vanilla display section
                    // We override its operand with our own label, so that it jumps were we want
                    // instead of where it's supposed to. This overrides vanilla display
                    instruction.operand = jumpOverSourceDisplay;
                    yield return instruction;
                    seq++;
                    continue;
                }
                else if(seq == 2 && Seq2.IsMatchComplete(instruction)) // going in if Seq2 is not completed
                {
                    // We don't do anything when Seq2 is completed
                    // We just want the iterator to point at the right place
                    // ie. where List<bool> is manipulated
                    seq++;
                    // still keeping a ref to stop instruction because we patch right before
                    seq2Last = instruction;
                    continue;
                }
                else if(seq > 2 && !patched)
                {   
                    /*
                        1.  We load the list of ingredient on the stack,
                            and we add our label to complete the override.
                            With this, we finalize the jump over the section
                            displaying vanilla menu.
                        
                        2.  We load the list of bool representing selected
                            ingredients on stack.

                        3.  We call our custom screen method, with the last
                            two list as parameters.
                        
                        4.  We push back Seq2 last instruction, to keep
                            original logic intact.


                        TODO ==============================================
                        Add a check on our method return to act according
                        to selection.
                        ie. actually cook with selection if any, or just quit 
                            menu (and maybe return to campfire selection popup)
                    */
                    Label dontQuit = gen.DefineLabel();
                    CodeInstruction loadIngredientList = new CodeInstruction(OpCodes.Ldloc_3);
                    loadIngredientList.labels.Add(jumpOverSourceDisplay);
                    yield return loadIngredientList; // Push ingredient List on stack
                    yield return Seq1.MatchedInstructions[3].Clone(); // Push
                    yield return new CodeInstruction(OpCodes.Call, QudUX_IngredientSelectionScreen_Static_Show);
                    yield return new CodeInstruction(OpCodes.Brtrue_S, dontQuit); // Check our ret value. If > 1, jmp to dontQuit
                    yield return new CodeInstruction(OpCodes.Ldc_I4_0); // if < 1, load 0 on stack (false) and return
                    yield return new CodeInstruction(OpCodes.Ret);
                    yield return seq2Last.WithLabels(dontQuit);
                    patched = true;
                }

                yield return instruction;
            }

            ReportPatchStatus("CookFromIngredients", patched);
        }

        [HarmonyTranspiler]
        [HarmonyPatch("CookFromRecipe")]
        static IEnumerable<CodeInstruction> Transpiler_CookFromRecipe(IEnumerable<CodeInstruction> instructions, ILGenerator gen)
        {
            /*
                Vanilla CookFromRecipe (1.0+):
                    List<Tuple<string, CookingRecipe>> recipes = new ...;   // filled from known recipes
                    if (recipes.Count > 0) goto pickRecipe; else "You don't know any recipes."
                  pickRecipe:
                    index = Popup.PickOption(...); then a Cook / Favorite / Forget / Back sub-menu ...
                  cook:
                    List<GameObject> used = new List<GameObject>();
                    if (!recipes[index].Item2.UseIngredients(used)) ...
                We send the "Count > 0" branch to our screen instead, which handles favorite/forget
                itself and returns the chosen index (or < 0 to back out), then continue at "cook".
                Vanilla's UseIngredients therefore still consumes the ingredients.
                Anchors are typed operands (the recipe list's constructor, List.get_Count,
                List<GameObject>'s constructor), not instruction gaps.

                The recipe list's element type differs by build: Tuple<string, CookingRecipe> on 211,
                Tuple<string, JournalRecipeNote> on 212+. It is read from the list constructor at patch
                time; non-CookingRecipe lists go through ShowForRecipeNotes<T>, which unwraps each
                entry's Recipe for our screen and maps the choice back to vanilla's list.
            */
            var codes = instructions.ToList();
            bool patched = false;
            try
            {
                var matcher = new CodeMatcher(codes, gen)
                    .MatchStartForward(
                        new CodeMatch(ci => ci.opcode == OpCodes.Newobj && ci.operand is ConstructorInfo c && IsRecipeListType(c.DeclaringType)),
                        new CodeMatch(ci => ci.IsStloc()));
                if (matcher.IsValid)
                {
                    Type recipeListType = ((ConstructorInfo)matcher.Operand).DeclaringType;
                    Type entryType = recipeListType.GetGenericArguments()[0].GetGenericArguments()[1];
                    MethodInfo showScreenMethod = entryType == typeof(XRL.World.Skills.Cooking.CookingRecipe)
                        ? QudUX_RecipeSelectionScreen_Static_Show
                        : AccessTools.Method(typeof(Patch_XRL_World_Parts_Campfire_Cooking), nameof(ShowForRecipeNotes)).MakeGenericMethod(entryType);
                    object recipeList = matcher.InstructionAt(1).operand;
                    matcher.MatchStartForward(
                        new CodeMatch(ci => ci.IsLdloc() && Equals(ci.operand, recipeList)),
                        new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(recipeListType, "Count")),
                        new CodeMatch(OpCodes.Ldc_I4_0),
                        new CodeMatch(ci => ci.opcode == OpCodes.Bgt || ci.opcode == OpCodes.Bgt_S));
                    if (matcher.IsValid)
                    {
                        int countBranch = matcher.Pos + 3;
                        matcher.MatchStartForward(
                            new CodeMatch(OpCodes.Newobj, List_GameObject_ctor),
                            new CodeMatch(ci => ci.IsStloc()),
                            new CodeMatch(ci => ci.IsLdloc() && Equals(ci.operand, recipeList)),
                            new CodeMatch(ci => ci.IsLdloc()),
                            new CodeMatch(OpCodes.Callvirt, AccessTools.Method(recipeListType, "get_Item")));
                        if (matcher.IsValid)
                        {
                            object recipeIndex = matcher.InstructionAt(3).operand;
                            Label cook = gen.DefineLabel();
                            Label showScreen = gen.DefineLabel();
                            Label chosen = gen.DefineLabel();
                            matcher.AddLabels(new[] { cook });
                            matcher.Insert(
                                new CodeInstruction(OpCodes.Br, cook), // vanilla fall-through into "cook" skips our block
                                new CodeInstruction(OpCodes.Ldloc_S, recipeList).WithLabels(showScreen),
                                new CodeInstruction(OpCodes.Call, showScreenMethod),
                                new CodeInstruction(OpCodes.Stloc_S, recipeIndex),
                                new CodeInstruction(OpCodes.Ldloc_S, recipeIndex),
                                new CodeInstruction(OpCodes.Ldc_I4_0),
                                new CodeInstruction(OpCodes.Bge, chosen),
                                new CodeInstruction(OpCodes.Ldc_I4_0), // backed out of our screen: return false
                                new CodeInstruction(OpCodes.Ret),
                                new CodeInstruction(OpCodes.Br, cook).WithLabels(chosen));
                            codes = matcher.InstructionEnumeration().ToList();
                            codes[countBranch].opcode = OpCodes.Bgt;
                            codes[countBranch].operand = showScreen;
                            patched = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.Logger.Log("[CookFromRecipe Patch] " + ex);
                codes = instructions.ToList();
                patched = false;
            }
            ReportPatchStatus("CookFromRecipe", patched);
            return patched ? codes : instructions;
        }
        /// <summary>List&lt;Tuple&lt;string, T&gt;&gt; where T is a CookingRecipe or wraps one in a Recipe member.</summary>
        private static bool IsRecipeListType(Type t)
        {
            if (t == null || !t.IsGenericType || t.GetGenericTypeDefinition() != typeof(List<>)) return false;
            Type tuple = t.GetGenericArguments()[0];
            if (!tuple.IsGenericType || tuple.GetGenericTypeDefinition() != typeof(Tuple<,>)) return false;
            Type[] args = tuple.GetGenericArguments();
            return args[0] == typeof(string) && RecipeAccessor(args[1]) != null;
        }

        private static Func<object, XRL.World.Skills.Cooking.CookingRecipe> RecipeAccessor(Type entryType)
        {
            if (entryType == typeof(XRL.World.Skills.Cooking.CookingRecipe)) return o => (XRL.World.Skills.Cooking.CookingRecipe)o;
            FieldInfo field = AccessTools.Field(entryType, "Recipe");
            if (field != null && field.FieldType == typeof(XRL.World.Skills.Cooking.CookingRecipe)) return o => (XRL.World.Skills.Cooking.CookingRecipe)field.GetValue(o);
            PropertyInfo prop = AccessTools.Property(entryType, "Recipe") ?? AccessTools.Property(entryType, "recipe");
            if (prop != null && prop.PropertyType == typeof(XRL.World.Skills.Cooking.CookingRecipe)) return o => (XRL.World.Skills.Cooking.CookingRecipe)prop.GetValue(o);
            return null;
        }

        /// <summary>
        /// Build 212+: vanilla's list holds JournalRecipeNote entries. Show our screen over their recipes, then
        /// return the index of the chosen recipe in vanilla's list (our screen sorts and filters its own copy).
        /// </summary>
        public static int ShowForRecipeNotes<TNote>(List<Tuple<string, TNote>> notes)
        {
            var recipeOf = RecipeAccessor(typeof(TNote));
            var recipes = notes.Select(n => Tuple.Create(n.Item1, recipeOf(n.Item2))).Where(t => t.Item2 != null).ToList();
            int chosen = XRL.UI.QudUX_RecipeSelectionScreen.Static_Show(recipes);
            if (chosen < 0 || chosen >= recipes.Count) return -1;
            var recipe = recipes[chosen].Item2;
            return notes.FindIndex(n => ReferenceEquals(recipeOf(n.Item2), recipe));
        }

        private static readonly Dictionary<string, bool> PatchStatuses = new Dictionary<string, bool>();
        private static void ReportPatchStatus(string patchname, bool success)
        {
            PatchStatuses.Add(patchname, success);
            if (PatchStatuses.Count >= 2)
            {
                int failCount = PatchStatuses.Where(s => !s.Value).Count();
                if (failCount > 0)
                {
                    string msg = "both";
                    if(failCount < 2)
                    {
                        msg = PatchStatuses.Where(s => !s.Value).ToArray()[0].Key;
                    }
                    PatchHelpers.LogPatchResult("Campfire",
                        $"Failed ({msg}). This patch may not be compatible with the current game version. "
                        + "The revamped cooking menus won't be used for the failed method(s).");
                }
                else
                {
                    PatchHelpers.LogPatchResult("Campfire",
                        "Patched successfully." /* Replaces the "cook with ingredients" and "choose recipe" menus. */ );
                }
                PatchStatuses.Clear();
            }
        }

        [HarmonyFinalizer]
        [HarmonyPatch("CookFromIngredients")]
        static void Finalizer(System.Exception __exception)
        {
            if(__exception != null)
            {
                Utilities.Logger.Log("[CookFromIngredients Patch] " + __exception.Message + "\n" + __exception.StackTrace) ;
            }
        }

    }


    /// <summary>
    /// I don't really know why, but without this patch, modifications
    /// made to the Cook with recipe methods have chances to just crash
    /// at the very end, because it calls Campfire.ForceTastyBasedOnIngredients
    /// with a list without initializing it ? I don't know why vanilla
    /// isn't crashing.
    /// </summary>
    [HarmonyPatch(typeof(XRL.World.Parts.Campfire))]
    public class Patch_XRL_World_Parts_Campfire_ForceTastyBasedOnIngredients
    {
        [HarmonyTargetMethod]
        static MethodInfo TargetMethod()
        {
            return AccessTools.TypeByName("XRL.World.Parts.Campfire").GetMethod("ForceTastyBasedOnIngredients", new Type[]
            {
                typeof(List<GameObject>)
            });
        }

        [HarmonyPrefix]
        static bool Prefix(List<GameObject> ingredientObjects)
        {
            return ingredientObjects != null;
        }
    }
}
