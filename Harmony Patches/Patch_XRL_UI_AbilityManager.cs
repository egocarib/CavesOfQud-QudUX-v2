using System.Linq;
using System.Reflection.Emit;
using System.Collections.Generic;
using HarmonyLib;
using static QudUX.Concepts.Constants.MethodsAndFields;

namespace QudUX.HarmonyPatches
{
    /// <summary>
    /// The following patch makes some modifications to the Manage Abilities screen.The main intent
    /// is to call our own custom function that improves the descriptions of many abilities or adds
    /// descriptions when the base game doesn't provide any.
    /// </summary>
    [HarmonyPatch(typeof(XRL.UI.AbilityManager))]
    public class Patch_XRL_UI_AbilityManager
    {
        private readonly static List<CodeInstruction> FirstSegmentTargetInstructions = new List<CodeInstruction>()
        {
            new CodeInstruction(OpCodes.Ldstr, "You have no abilities to manage!"),
            new CodeInstruction(OpCodes.Ldfld, AbilityNode_Ability)
        };
        private readonly static CodeInstruction SecondSegmentTargetInstruction = new CodeInstruction(OpCodes.Newobj, TextBlock_ctor);
        private static int AllowedInstructionDistance = 20; //allowed distance between instructions in target sequence

        [HarmonyTranspiler]
        [HarmonyPatch("Show")]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            int idx = 0;
            int gap = 0;
            int patchSegment = 1;
            bool found = false;
            CodeInstruction instruction_LoadLocal_AbilityNodeArray = null;
            CodeInstruction instruction_LoadLocal_AbilityNodeIndex = null;
            CodeInstruction instruction_LoadLocal_TextBlock = null;
            bool patchComplete = false;
            foreach (var instruction in instructions)
            {
                if (found)
                {
                    if (patchSegment == 1)
                    {
                        if (idx == 0 && instruction.opcode == OpCodes.Brfalse_S)
                        {
                            idx++;
                            //do nothing, we're just confirming the expected instruction is here
                        }
                        else if (idx == 1 && PatchHelpers.IsLoadLocalInstruction(instruction))
                        {
                            instruction_LoadLocal_AbilityNodeArray = instruction.Clone();
                            idx++;
                        }
                        else if (idx == 2 && PatchHelpers.IsLoadLocalInstruction(instruction))
                        {
                            instruction_LoadLocal_AbilityNodeIndex = instruction.Clone();
                            found = false;
                            idx = 0;
                            patchSegment++;
                        }
                    }
                    else if (!patchComplete)
                    {
                        if (idx == 0 && instruction.opcode == OpCodes.Stloc_S)
                        {
                            idx++;
                            instruction_LoadLocal_TextBlock = new CodeInstruction(OpCodes.Ldloc_S, instruction.operand);
                        }
                        else if (idx == 1)
                        {
                            //inject our patch code
                            //get the current AbilityNode item from the array and load it onto the stack
                            yield return instruction_LoadLocal_AbilityNodeArray;
                            yield return instruction_LoadLocal_AbilityNodeIndex;
                            yield return new CodeInstruction(OpCodes.Callvirt, List_AbilityNode_get_Item);
                            //load the TextBlock onto the stack before the game uses it to write to the screen
                            yield return instruction_LoadLocal_TextBlock;
                            //call our custom function to update the text block
                            yield return new CodeInstruction(OpCodes.Call, AbilityManagerExtender_UpdateAbilityText);

                            PatchHelpers.LogPatchResult("AbilityManager",
                                "Patched successfully." /* Improves activated ability descriptions and cooldown information on the Manage Abilities screen. */ );
                            patchComplete = true;
                        }
                    }
                }
                else if (patchSegment == 1)
                {
                    if (PatchHelpers.InstructionsAreEqual(instruction, FirstSegmentTargetInstructions[idx]))
                    {
                        if (++idx == FirstSegmentTargetInstructions.Count())
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
                else if (patchSegment == 2)
                {
                    if (PatchHelpers.InstructionsAreEqual(instruction, SecondSegmentTargetInstruction))
                    {
                        found = true;
                    }
                }
                yield return instruction;
            }
            if (patchComplete == false)
            {
                PatchHelpers.LogPatchResult("AbilityManager",
                    "Failed. This patch may not be compatible with the current game version. Improved activated ability descriptions and cooldown information won't be added to the Manage Abilities screen.");
            }
        }
    }
}
