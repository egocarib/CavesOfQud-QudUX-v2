using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using HarmonyLib;
using static QudUX.HarmonyPatches.PatchHelpers;
using static QudUX.Concepts.Constants.MethodsAndFields;
using Options = QudUX.Concepts.Options;

namespace QudUX.HarmonyPatches
{
    [HarmonyPatch(typeof(XRL.UI.CreateCharacter))]
    class Patch_XRL_UI_CreateCharacter
    {
        [HarmonyPrepare]
        static bool Prepare(MethodBase original)
        {
            if (original == null)
            {
                return true;
            }
            if (original.Name == "ShowCreateCharacter" && !Options.UI.UseQudUXBuildLibrary)
            {
                PatchHelpers.LogPatchResult("CreateCharacter",
                    "Skipped. The \"Use revamped build library\" option is disabled.");
                return false;
            }
            if (original.Name == "GenerateCharacter" && !Options.UI.UseSpriteMenu)
            {
                PatchHelpers.LogPatchResult("GenerateCharacter",
                    "Skipped. Adds a menu for choosing your character sprite during character creation.");
                return false;
            }
            return true;
        }

        [HarmonyTranspiler]
        [HarmonyPatch("ShowCreateCharacter")]
        static IEnumerable<CodeInstruction> Transpiler_ShowCreateCharacter(IEnumerable<CodeInstruction> instructions)
        {
            var Sequence = new PatchTargetInstructionSet(new List<PatchTargetInstruction>
            {
                new PatchTargetInstruction(OpCodes.Call, CreateCharacter_BuildLibraryManagement)
            });

            bool patched = false;
            foreach (var instruction in instructions)
            {
                if (!patched && Sequence.IsMatchComplete(instruction))
                {
                    instruction.operand = QudUX_BuildLibraryScreen_Show;
                    patched = true;
                }
                yield return instruction;
            }

            if (patched)
            {
                PatchHelpers.LogPatchResult("CreateCharacter",
                    "Patched successfully." /* Revamps the Build Library text UI. */ );
            }
            else
            {
                PatchHelpers.LogPatchResult("CreateCharacter",
                    "Failed. This patch may not be compatible with the current game version. "
                    + "The revamped Build Library text UI will not be used.");
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("GenerateCharacter")]
        static void Prefix()
        {
            ScreenExtenders.CreateCharacterExtender.ResetTileInfo();
        }

        [HarmonyPrefix]
        [HarmonyPatch("GenerateCharacter")]
        static void Postfix()
        {
            ScreenExtenders.CreateCharacterExtender.CreateCharacterBuffer = null;
            ConsoleLib.Console.ScreenBuffer.ClearImposterSuppression();
        }

        [HarmonyTranspiler]
        [HarmonyPatch("GenerateCharacter")]
        static IEnumerable<CodeInstruction> Transpiler_GenerateCharacter(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var Sequence1 = new PatchTargetInstructionSet(new List<PatchTargetInstruction>
            {
                new PatchTargetInstruction(OpCodes.Ldstr, "StartingPet"),
                new PatchTargetInstruction(OpCodes.Callvirt, List_GameObjectBlueprint_get_Count, 1),
                new PatchTargetInstruction(OpCodes.Ldsfld, CreateCharacter__Console, 35),
                new PatchTargetInstruction(OpCodes.Ldarg_0, 0),
                new PatchTargetInstruction(OpCodes.Ldnull, 0),
                new PatchTargetInstruction(OpCodes.Ldc_I4_0, 0),
                new PatchTargetInstruction(OpCodes.Callvirt, TextConsole_DrawBuffer, 0)
            });
            var Sequence2 = new PatchTargetInstructionSet(new List<PatchTargetInstruction>
            {
                new PatchTargetInstruction(OpCodes.Ldc_I4_S, (object)84),
                new PatchTargetInstruction(OpCodes.Bne_Un_S, 0),
                new PatchTargetInstruction(OpCodes.Ldstr, "HEY! Try my Caves of Qud character build.\n", 0),
                new PatchTargetInstruction(IsLoadLocalInstruction, 5),
                new PatchTargetInstruction(OpCodes.Ldc_I4_S, (object)83)
            });

            int seq = 1;
            bool patched = false;
            foreach (var instruction in instructions)
            {
                if (seq == 1)
                {
                    if (Sequence1.IsMatchComplete(instruction))
                    {
                        yield return Sequence1.MatchedInstructions[3].Clone();
                        yield return new CodeInstruction(OpCodes.Call, CreateCharacterExtender_WriteCharCreateSpriteOptionText);
                        seq++;
                    }
                }
                else if (!patched && seq == 2)
                {
                    if (Sequence2.IsMatchComplete(instruction))
                    {
                        //here we are essentially adding:
                        //   if (keys == Keys.M)
                        //   {
                        //       CreateCharacterExtender.PickCharacterTile();
                        //       _Console.DrawBuffer(SB);
                        //   }

                        yield return new CodeInstruction(OpCodes.Ldc_I4_S, 77); //Keys.M
                        Label newLabel = generator.DefineLabel();
                        yield return new CodeInstruction(OpCodes.Bne_Un_S, newLabel);
                        yield return new CodeInstruction(OpCodes.Ldsfld, CreateCharacter_Template);
                        yield return new CodeInstruction(OpCodes.Call, CreateCharacterExtender_PickCharacterTile);

                        //redraw the buffer over the screen we made
                        yield return Sequence1.MatchedInstructions[2].Clone();
                        yield return Sequence1.MatchedInstructions[3].Clone();
                        yield return Sequence1.MatchedInstructions[4].Clone();
                        yield return Sequence1.MatchedInstructions[5].Clone();
                        yield return Sequence1.MatchedInstructions[6].Clone();
                        
                        CodeInstruction markedLoadLocal = Sequence2.MatchedInstructions[3].Clone();
                        markedLoadLocal.labels.Add(newLabel);
                        yield return markedLoadLocal;

                        patched = true;
                    }
                }
                yield return instruction;
            }

            if (patched)
            {
                PatchHelpers.LogPatchResult("GenerateCharacter",
                    "Patched successfully." /* Adds a menu for choosing your character sprite during character creation. */ );
            }
            else
            {
                PatchHelpers.LogPatchResult("GenerateCharacter",
                    "Failed. This patch may not be compatible with the current game version. "
                    + "The character sprite customization menu won't be available during character creation.");
            }
        }
    }
}
