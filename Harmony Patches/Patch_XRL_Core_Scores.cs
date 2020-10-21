using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using static QudUX.HarmonyPatches.PatchHelpers;
using static QudUX.Concepts.Constants.MethodsAndFields;

namespace QudUX.HarmonyPatches
{
    [HarmonyPatch(typeof(XRL.Core.Scores))]
    class Patch_XRL_Core_Scores
    {
        [HarmonyTranspiler]
        [HarmonyPatch("Show")]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var Sequence1 = new PatchTargetInstructionSet(new List<PatchTargetInstruction>
            {
                new PatchTargetInstruction(OpCodes.Ldc_I4_S, (object)62),
                new PatchTargetInstruction(OpCodes.Ldc_I4_S, (object)24, 0),
                new PatchTargetInstruction(OpCodes.Callvirt, 0), //ScreenBuffer.Goto
                new PatchTargetInstruction(OpCodes.Pop, 0),
                new PatchTargetInstruction(OpCodes.Ldsfld, 0),
                new PatchTargetInstruction(LoadHighScoreDeleteCommandString, 0),
                new PatchTargetInstruction(OpCodes.Ldc_I4_1, 0),
                new PatchTargetInstruction(OpCodes.Callvirt, ScreenBuffer_Write, 0),
                new PatchTargetInstruction(OpCodes.Pop, 0),
            });
            var Sequence2 = new PatchTargetInstructionSet(new List<PatchTargetInstruction>
            {
                new PatchTargetInstruction(OpCodes.Ldsfld), //continues immediately after the previous sequence
                new PatchTargetInstruction(OpCodes.Ldsfld, 0),
                new PatchTargetInstruction(OpCodes.Ldnull, 0),
                new PatchTargetInstruction(OpCodes.Ldc_I4_1, 0),
                new PatchTargetInstruction(OpCodes.Callvirt, TextConsole_DrawBuffer, 0)
            });
            var Sequence3 = new PatchTargetInstructionSet(new List<PatchTargetInstruction>
            {
                new PatchTargetInstruction(IsStoreLocalInstruction),
                new PatchTargetInstruction(IsLoadLocalInstruction, 0),
                new PatchTargetInstruction(OpCodes.Ldc_I4_S, (object)98, 0)
            });

            int seq = 1;
            bool patched = false;
            foreach (var instruction in instructions)
            {
                if (seq == 1)
                {
                    if (Sequence1.IsMatchComplete(instruction))
                    {
                        yield return new CodeInstruction(OpCodes.Ldc_I4_S, 58);
                        yield return new CodeInstruction(OpCodes.Ldc_I4_S, 23);
                        yield return Sequence1.MatchedInstructions[2].Clone();
                        yield return new CodeInstruction(OpCodes.Ldstr, "&Y[&WTab&y - Detailed Stats&Y]");
                        yield return Sequence1.MatchedInstructions[6].Clone();
                        yield return Sequence1.MatchedInstructions[7].Clone();
                        seq++;
                    }
                }
                else if (seq == 2)
                {
                    if (Sequence2.IsMatchComplete(instruction))
                    {
                        seq++;
                    }
                }
                else if (!patched && seq == 3)
                {
                    if (Sequence3.IsMatchComplete(instruction))
                    {
                        //here we are essentially adding:
                        //   if (keys == Keys.Tab)
                        //   {
                        //       EnhancedScoreboardExtender.ShowGameStatsScreen();
                        //       Console.DrawBuffer(Buffer, null, bSkipIfOverlay: true);
                        //   }

                        yield return new CodeInstruction(OpCodes.Ldc_I4_S, 9); //Keys.Tab
                        Label newLabel = generator.DefineLabel();
                        yield return new CodeInstruction(OpCodes.Bne_Un_S, newLabel);
                        yield return new CodeInstruction(OpCodes.Call, EnhancedScoreboardExtender_ShowGameStatsScreen);

                        //redraw the buffer over the screen we made
                        yield return Sequence2.MatchedInstructions[0].Clone();
                        yield return Sequence2.MatchedInstructions[1].Clone();
                        yield return Sequence2.MatchedInstructions[2].Clone();
                        yield return Sequence2.MatchedInstructions[3].Clone();
                        yield return Sequence2.MatchedInstructions[4].Clone();

                        CodeInstruction markedLoadLocal = Sequence3.MatchedInstructions[1].Clone();
                        markedLoadLocal.labels.Add(newLabel);
                        yield return markedLoadLocal;

                        patched = true;
                    }
                }
                yield return instruction;
            }

            if (patched)
            {
                PatchHelpers.LogPatchResult("Scores",
                    "Patched successfully." /* Adds an option to open a detailed game stats menu from the High Scores console UI. */ );
            }
            else
            {
                PatchHelpers.LogPatchResult("Scores",
                    "Failed. This patch may not be compatible with the current game version. "
                    + "The High Scores text UI won't have an option to open detailed stats.");
            }
        }

        public static bool LoadHighScoreDeleteCommandString(CodeInstruction i)
        {
            if (i.opcode == OpCodes.Ldstr)
            {
                string val = (string)i.operand;
                return !string.IsNullOrEmpty(val) && val.Contains("Del");
            }
            return false;
        }
    }
}
