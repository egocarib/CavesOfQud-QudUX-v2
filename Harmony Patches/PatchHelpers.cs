using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using HarmonyLib;

namespace QudUX.HarmonyPatches
{
    using Logger = QudUX.Utilities.Logger;

    public class PatchHelpers
    {
        public static void LogPatchResult(string patchName, string resultString)
        {
            patchName = $"{patchName}...".PadRight(22);
            Logger.LogCompiled("Patches", $"    {patchName}  {resultString}", "Applying pre-game patches...");
        }

        // Useful reference on opcode operand types: https://stackoverflow.com/questions/7212255
        public static bool InstructionsAreEqual(CodeInstruction i1, CodeInstruction i2)
        {
            if (i1.operand == null || i2.operand == null)
            {
                return i1.opcode == i2.opcode && i1.operand == i2.operand;
            }
            return i1.Is(i2.opcode, i2.operand); //recommended comparison method (https://bit.ly/2R7GkeA), but can't handle null
        }

        public static bool IsLoadLocalInstruction(CodeInstruction i)
        {
            return (i.opcode == OpCodes.Ldloc
                || i.opcode == OpCodes.Ldloc_0
                || i.opcode == OpCodes.Ldloc_1
                || i.opcode == OpCodes.Ldloc_2
                || i.opcode == OpCodes.Ldloc_3
                || i.opcode == OpCodes.Ldloc_S);
        }

        public static class ILBlocks
        {
            private static readonly MethodInfo Method_Options_GetOption = typeof(XRL.UI.Options).GetMethod("GetOption");
            private static readonly MethodInfo Method_String_Equality = typeof(string).GetMethod("op_Equality", new Type[] { typeof(string), typeof(string) });

            private static List<CodeInstruction> CheckOptionEqualsYes(string optionName)
            {
                List<CodeInstruction> instructions = new List<CodeInstruction>();
                instructions.Add(new CodeInstruction(OpCodes.Ldstr, optionName));
                instructions.Add(new CodeInstruction(OpCodes.Ldstr, string.Empty));
                instructions.Add(new CodeInstruction(OpCodes.Call, Method_Options_GetOption));
                instructions.Add(new CodeInstruction(OpCodes.Ldstr, "Yes"));
                instructions.Add(new CodeInstruction(OpCodes.Call, Method_String_Equality));
                return instructions;
            }

            public static List<CodeInstruction> IfOptionYesJumpTo(string optionName, object jumpTarget, bool shortForm = false)
            {
                List<CodeInstruction> instructions = CheckOptionEqualsYes(optionName);
                OpCode jumpOpCode = shortForm ? OpCodes.Brtrue_S : OpCodes.Brtrue;
                instructions.Add(new CodeInstruction(jumpOpCode, jumpTarget));
                return instructions;
            }

            public static List<CodeInstruction> IfOptionNotYesJumpTo(string optionName, object jumpTarget, bool shortForm = false)
            {
                List<CodeInstruction> instructions = CheckOptionEqualsYes(optionName);
                OpCode jumpOpCode = shortForm ? OpCodes.Brfalse_S : OpCodes.Brfalse;
                instructions.Add(new CodeInstruction(jumpOpCode, jumpTarget));
                return instructions;
            }

            public static List<CodeInstruction> IfOptionJumpTo_ElseJumpTo(string optionName, object yesJumpTarget, object notYesJumpTarget, bool shortForm = false)
            {
                List<CodeInstruction> instructions = CheckOptionEqualsYes(optionName);
                OpCode jumpOpCode = shortForm ? OpCodes.Brtrue_S : OpCodes.Brtrue;
                instructions.Add(new CodeInstruction(jumpOpCode, yesJumpTarget));
                instructions.Add(new CodeInstruction(jumpOpCode, notYesJumpTarget));
                return instructions;
            }
        }

        public class PatchTargetInstruction
        {
            public readonly CodeInstruction Instruction;
            public readonly Func<CodeInstruction, bool> InstructionDelegate;
            public readonly bool IgnoreOperand = false;
            public readonly int MaxGapFromPrior;

            public PatchTargetInstruction(Func<CodeInstruction, bool> instructionMatchProcessor, int maxGapFromPrior = 20)
                : this(OpCodes.Nop, null, maxGapFromPrior, true, instructionMatchProcessor) { }

            public PatchTargetInstruction(OpCode opcode, int maxGapFromPrior = 20)
                : this(opcode, null, maxGapFromPrior, true, null) { }

            public PatchTargetInstruction(OpCode opcode, object operand, int maxGapFromPrior = 20)
                : this(opcode, operand, maxGapFromPrior, false, null) { }

            private PatchTargetInstruction(OpCode opcode, object operand, int maxGapFromPrior, bool ignoreOperand, Func<CodeInstruction, bool> instructionMatchProcessor)
            {
                Instruction = new CodeInstruction(opcode, operand);
                IgnoreOperand = ignoreOperand;
                MaxGapFromPrior = maxGapFromPrior;
                InstructionDelegate = instructionMatchProcessor;
            }
        }

        public class PatchTargetInstructionSet
        {
            private readonly List<PatchTargetInstruction> Instructions;
            public CodeInstruction[] MatchedInstructions;
            private int CurrentIndex = 0;
            private int CurrentGap = 0;
            private bool Matched = false;

            public PatchTargetInstructionSet(List<PatchTargetInstruction> instructions)
            {
                Instructions = instructions;
                MatchedInstructions = new CodeInstruction[Instructions.Count];
            }

            public bool IsMatchComplete(CodeInstruction instruction, bool showDebugInfo = false)
            {
                if (Matched)
                {
                    throw new Exception("PatchTargetInstructionSet invoked after match was already made [QudUX]");
                }
                if (showDebugInfo)
                {
                    Logger.Log("PatchTargetInstructionSet Debug:"
                        + $"\n    IgnoreOperand={Instructions[CurrentIndex].IgnoreOperand}"
                        + $"\n    CurrentIndex={CurrentIndex}"
                        + $"\n    CurrentGap={CurrentGap}   [will fail after gap > {Instructions[CurrentIndex].MaxGapFromPrior}]"
                        + $"\n    instruction:  {instruction.opcode}   ==>   {instruction.operand}"
                        + $"\n    wantedMatch:  {Instructions[CurrentIndex].Instruction.opcode}   ==>   {Instructions[CurrentIndex].Instruction.operand}");
                }
                if (Instructions[CurrentIndex].IgnoreOperand)
                {
                    bool isMatch;
                    if (Instructions[CurrentIndex].InstructionDelegate != null)
                    {
                        isMatch = Instructions[CurrentIndex].InstructionDelegate(instruction);
                    }
                    else
                    {
                        isMatch = (instruction.opcode == Instructions[CurrentIndex].Instruction.opcode);
                    }

                    if (isMatch)
                    {
                        MatchedInstructions[CurrentIndex++] = instruction;
                        CurrentGap = 0;
                    }
                    else if (++CurrentGap > Instructions[CurrentIndex].MaxGapFromPrior)
                    {
                        CurrentIndex = 0;
                    }
                }
                else
                {
                    if (InstructionsAreEqual(instruction, Instructions[CurrentIndex].Instruction))
                    {
                        MatchedInstructions[CurrentIndex++] = instruction;
                        CurrentGap = 0;
                    }
                    else if (++CurrentGap > Instructions[CurrentIndex].MaxGapFromPrior)
                    {
                        CurrentIndex = 0;
                    }
                }
                if (CurrentIndex == Instructions.Count)
                {
                    return Matched = true;
                }
                return false;
            }
        }
    }
}
