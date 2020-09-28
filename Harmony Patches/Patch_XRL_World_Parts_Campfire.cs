using System.Linq;
using System.Reflection.Emit;
using System.Collections.Generic;
using HarmonyLib;
using QudUX.Concepts;
using static QudUX.Concepts.Constants.MethodsAndFields;

namespace QudUX.HarmonyPatches
{
    /// <summary>
    /// The following patch replaces the "cook with ingredients" and "choose recipe" selection screens
    /// with entirely new UI screens. This is a fairly complex transpiler that modifies a number of
    /// details in the Campfire class. Unfortunately the code that handles these menus is overly
    /// complex, as it uses a lot of looping pop-ups instead of real menus.
    /// </summary>
    [HarmonyPatch(typeof(XRL.World.Parts.Campfire))]
    public class Patch_XRL_World_Parts_Campfire
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

        [HarmonyTranspiler]
        [HarmonyPatch("Cook")]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
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
    }
}
