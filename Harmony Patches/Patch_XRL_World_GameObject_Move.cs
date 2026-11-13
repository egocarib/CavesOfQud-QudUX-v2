using System;
using System.Linq;
using System.Reflection.Emit;
using System.Collections.Generic;
using HarmonyLib;
using static QudUX.HarmonyPatches.PatchHelpers;
using static QudUX.Concepts.Constants.MethodsAndFields;
using System.Reflection;
using XRL.World;

namespace QudUX.HarmonyPatches
{
    [HarmonyPatch]
    class Patch_XRL_World_GameObject_Move
    {
        static MethodInfo TargetMethod()
        {
            List<MethodInfo> gameObjectMethod = AccessTools.GetDeclaredMethods(typeof(XRL.World.GameObject));
            foreach(MethodInfo method in gameObjectMethod)
            {
                if(method.Name == "Move")
                {
                    ParameterInfo[] arguments = method.GetParameters();

                    if(arguments.Length > 2 && arguments[0].ParameterType == typeof(string) && arguments[1].ParameterType == typeof(GameObject).MakeByRefType())
                    {
                        return method;
                    }
                }
            }
            
            return null;
        }


        [HarmonyTranspiler]
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
            ReportPatchStatus("First part", patched);
        }

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler_Move2(IEnumerable<CodeInstruction> instructions)
        {
            // Movement into liquid asks for confirmation, then records it with
            //     The.Core.MoveConfirmDirection = Direction;
            // Right after that store we emit particle text for the liquid:
            //  - deep liquid (swimming): the local holding Cell.GetSwimmingDepthLiquid(), isDeepLiquid = true
            //  - dangerous liquid:       the local holding Cell.GetDangerousOpenLiquidVolume(), isDeepLiquid = false
            // The confirmation messages themselves changed (string.Concat in build 211, localized templates in
            // 212+), so the anchors are the liquid lookups and the MoveConfirmDirection store, not message text.
            var codes = instructions.ToList();
            var swimming = AccessTools.Method(typeof(Cell), nameof(Cell.GetSwimmingDepthLiquid));
            var dangerous = AccessTools.Method(typeof(Cell), nameof(Cell.GetDangerousOpenLiquidVolume));

            int search = 0;
            bool deepPatched = TryEmitAfterConfirm(codes, swimming, true, ref search);
            bool dangerousPatched = deepPatched && TryEmitAfterConfirm(codes, dangerous, false, ref search);

            ReportPatchStatus("second part", deepPatched && dangerousPatched);
            return deepPatched && dangerousPatched ? codes : instructions;
        }

        /// <summary>
        /// From <paramref name="search"/>, find the next call to <paramref name="liquidLookup"/>, the local its
        /// result is stored in, and the next store to XRLCore.MoveConfirmDirection; insert
        /// ParticleTextMaker.EmitFromPlayerIfLiquid(liquid, isDeepLiquid) right after that store.
        /// </summary>
        private static bool TryEmitAfterConfirm(List<CodeInstruction> codes, MethodInfo liquidLookup, bool isDeepLiquid, ref int search)
        {
            int call = codes.FindIndex(search, ci => ci.Calls(liquidLookup));
            if (call < 0) return false;
            int store = codes.FindIndex(call + 1, ci => ci.IsStloc());
            if (store < 0 || store > call + 2) return false; // allow an intervening dup
            int confirm = codes.FindIndex(store + 1, ci => ci.StoresField(XRLCore_MoveConfirmDirection));
            if (confirm < 0) return false;

            codes.InsertRange(confirm + 1, new[]
            {
                LoadLocalFor(codes[store]),
                new CodeInstruction(isDeepLiquid ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Call, ParticleTextMaker_EmitFromPlayerIfLiquid),
            });
            search = confirm + 4;
            return true;
        }

        private static CodeInstruction LoadLocalFor(CodeInstruction store)
        {
            if (store.opcode == OpCodes.Stloc_0) return new CodeInstruction(OpCodes.Ldloc_0);
            if (store.opcode == OpCodes.Stloc_1) return new CodeInstruction(OpCodes.Ldloc_1);
            if (store.opcode == OpCodes.Stloc_2) return new CodeInstruction(OpCodes.Ldloc_2);
            if (store.opcode == OpCodes.Stloc_3) return new CodeInstruction(OpCodes.Ldloc_3);
            return new CodeInstruction(OpCodes.Ldloc_S, store.operand);
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
                    if (failCount < 2)
                    {
                        msg = PatchStatuses.Where(s => !s.Value).ToArray()[0].Key;
                    }
                    PatchHelpers.LogPatchResult("GameObject.Move",
                        $"Failed ({msg}). This patch may not be compatible with the current game version. "
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
