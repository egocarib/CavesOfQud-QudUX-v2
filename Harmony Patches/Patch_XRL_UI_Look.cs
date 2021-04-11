using System.Reflection.Emit;
using System.Collections.Generic;
using HarmonyLib;
using static QudUX.HarmonyPatches.PatchHelpers;
using static QudUX.Concepts.Constants.MethodsAndFields;

namespace QudUX.HarmonyPatches
{
    [HarmonyPatch(typeof(XRL.UI.Look))]
    public class Patch_XRL_UI_Look
    {
        [HarmonyTranspiler]
        [HarmonyPatch("ShowLooker")]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var Sequence1 = new PatchTargetInstructionSet(new List<PatchTargetInstruction>
            {
                new PatchTargetInstruction(OpCodes.Ldstr, "CmdWalk"),
                new PatchTargetInstruction(OpCodes.Ldstr, " | {{hotkey|", 8),
                new PatchTargetInstruction(OpCodes.Ldsfld, Look_Buffer, 30),
                new PatchTargetInstruction(OpCodes.Ldloc_S, 0),
                new PatchTargetInstruction(OpCodes.Ldloc_S, 1),
                new PatchTargetInstruction(OpCodes.Callvirt, ScreenBuffer_WriteAt, 1),
                new PatchTargetInstruction(OpCodes.Pop, 0),
                new PatchTargetInstruction(OpCodes.Ldloc_S, 0),
                new PatchTargetInstruction(OpCodes.Brfalse, 0),
                new PatchTargetInstruction(OpCodes.Ldloc_S, 0),
                new PatchTargetInstruction(OpCodes.Brfalse, 0)
            });
            var Sequence2 = new PatchTargetInstructionSet(new List<PatchTargetInstruction>
            {
                new PatchTargetInstruction(OpCodes.Ldloc_S),
                new PatchTargetInstruction(OpCodes.Ldc_I4_S, (object)101, 0),
                new PatchTargetInstruction(OpCodes.Beq_S, 0),
                new PatchTargetInstruction(OpCodes.Ldloc_S, 0),
                new PatchTargetInstruction(OpCodes.Ldc_I4_S, (object)27, 0),
                new PatchTargetInstruction(OpCodes.Bne_Un_S, 0),
                new PatchTargetInstruction(OpCodes.Ldc_I4_1, 0),
                new PatchTargetInstruction(OpCodes.Stloc_1, 0),
                new PatchTargetInstruction(OpCodes.Ldloc_S, 0)
            });

            int seq = 1;
            bool patched = false;
            foreach (var instruction in instructions)
            {
                if (seq == 1)
                {
                    if (Sequence1.IsMatchComplete(instruction))
                    {
                        yield return instruction;
                        yield return Sequence1.MatchedInstructions[2].Clone(); //load ScreenBuffer
                        yield return Sequence1.MatchedInstructions[7].Clone(); //load target GameObject
                        yield return Sequence1.MatchedInstructions[4].Clone(); //load Look UI hotkey string
                        yield return new CodeInstruction(OpCodes.Call, LookExtender_AddMarkLegendaryOptionToLooker);
                        seq++;
                        continue;
                    }
                }
                else if (!patched)
                {
                    if (Sequence2.IsMatchComplete(instruction))
                    {
                        CodeInstruction replacementInstruction = instruction.Clone();
                        instruction.opcode = Sequence2.MatchedInstructions[0].opcode; 
                        instruction.operand = Sequence2.MatchedInstructions[0].operand;
                        yield return instruction; //load pressed key onto stack (with existing label)
                        yield return Sequence1.MatchedInstructions[7].Clone(); //load target GameObject
                        yield return new CodeInstruction(OpCodes.Ldloc_1); //load flag on the stack
                        yield return new CodeInstruction(OpCodes.Call, LookExtender_CheckKeyPress); //call our method, which puts a bool on stack
                        yield return new CodeInstruction(OpCodes.Stloc_1); //store bool into flag
                        yield return replacementInstruction; //return original instruction (without label)
                        patched = true;
                        continue;
                    }
                }
                yield return instruction;
            }
            if (patched)
            {
                PatchHelpers.LogPatchResult("Look",
                    "Patched successfully." /* Adds option to mark legendary creature locations in the journal from the Look UI. */ );
            }
            else
            {
                PatchHelpers.LogPatchResult("Look",
                    "Failed. This patch may not be compatible with the current game version. "
                    + "The option to mark legendary creature locations in journal won't be available from the Look UI.");
            }
        }
    }
}
