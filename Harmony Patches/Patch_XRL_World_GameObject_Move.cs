using System.Linq;
using System.Reflection.Emit;
using System.Collections.Generic;
using HarmonyLib;
using static QudUX.HarmonyPatches.PatchHelpers;
using static QudUX.Concepts.Constants.MethodsAndFields;

namespace QudUX.HarmonyPatches
{
    [HarmonyPatch(typeof(XRL.World.GameObject))]
    class Patch_XRL_World_GameObject_Move
    {
        [HarmonyTranspiler]
        [HarmonyPatch("Move")]
        static IEnumerable<CodeInstruction> Transpiler_Move1(IEnumerable<CodeInstruction> instructions)
        {
            var Sequence = new PatchTargetInstructionSet(new List<PatchTargetInstruction>
            {
                new PatchTargetInstruction(OpCodes.Ldstr, "You cannot go that way."),
                new PatchTargetInstruction(OpCodes.Call, MessageQueue_AddPlayerMessage, 2)
            });

            bool patched = false;
            foreach (var instruction in instructions)
            {
                yield return instruction;
                if (!patched && Sequence.IsMatchComplete(instruction))
                {
                    yield return new CodeInstruction(OpCodes.Ldstr, "{{w|Can't go that way!}},{{w|Nothing there!}}");
                    yield return new CodeInstruction(OpCodes.Call, ParticleTextMaker_EmitFromPlayer);
                    patched = true;
                }
            }
            ReportPatchStatus(patched);
        }

        [HarmonyTranspiler]
        [HarmonyPatch("Move")]
        static IEnumerable<CodeInstruction> Transpiler_Move2(IEnumerable<CodeInstruction> instructions)
        {
            var Sequence1 = new PatchTargetInstructionSet(new List<PatchTargetInstruction>
            {
                new PatchTargetInstruction(OpCodes.Callvirt, Cell_HasBridge),
                new PatchTargetInstruction(OpCodes.Callvirt, GameObject_IsDangerousOpenLiquidVolume, 80),
                new PatchTargetInstruction(OpCodes.Ldstr, ", dangerous-looking ", 30),
                new PatchTargetInstruction(OpCodes.Ldloc_S, 8),
                new PatchTargetInstruction(OpCodes.Callvirt, GameObject_get_ShortDisplayName, 0),
                new PatchTargetInstruction(OpCodes.Stfld, XRLCore_MoveConfirmDirection, 40)
            });
            var Sequence2 = new PatchTargetInstructionSet(new List<PatchTargetInstruction>
            {
                new PatchTargetInstruction(OpCodes.Callvirt, Cell_GetDangerousOpenLiquidVolume),
                new PatchTargetInstruction(OpCodes.Ldfld, XRLCore_MoveConfirmDirection, 6),
                new PatchTargetInstruction(OpCodes.Callvirt, GameObject_get_the, 62),
                new PatchTargetInstruction(OpCodes.Ldloc_S, 4),
                new PatchTargetInstruction(OpCodes.Callvirt, GameObject_get_ShortDisplayName, 0),
                new PatchTargetInstruction(OpCodes.Stfld, XRLCore_MoveConfirmDirection, 22)
            });

            int seq = 1;
            bool patched = false;
            foreach (var instruction in instructions)
            {
                yield return instruction;
                if (seq == 1)
                {
                    if (Sequence1.IsMatchComplete(instruction))
                    {
                        yield return new CodeInstruction(OpCodes.Ldloc_S, Sequence1.MatchedInstructions[3].operand);
                        yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                        yield return new CodeInstruction(OpCodes.Call, ParticleTextMaker_EmitFromPlayerIfLiquid);
                        seq++;
                    }
                }
                else if (!patched && Sequence2.IsMatchComplete(instruction))
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_S, Sequence2.MatchedInstructions[3].operand);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                    yield return new CodeInstruction(OpCodes.Call, ParticleTextMaker_EmitFromPlayerIfLiquid);
                    patched = true;
                }
            }
            ReportPatchStatus(patched);
        }

        private static readonly List<bool> PatchStatuses = new List<bool>();
        private static void ReportPatchStatus(bool success)
        {
            PatchStatuses.Add(success);
            if (PatchStatuses.Count >= 2)
            {
                int failCount = PatchStatuses.Where(s => s == false).Count();
                if (failCount > 0)
                {
                    PatchHelpers.LogPatchResult("GameObject.Move",
                        $"Failed ({failCount}/2). This patch may not be compatible with the current game version. "
                        + "Some particle text effects may not be shown when movement is prevented.");
                }
                else
                {
                    PatchHelpers.LogPatchResult("GameObject.Move",
                        "Patched successfully." /* Adds option to show particle text messages when movement is prevented for various reasons. */ );
                }
                PatchStatuses.Clear();
            }
        }
    }
}
