using System.Linq;
using System.Reflection.Emit;
using System.Collections.Generic;
using HarmonyLib;
using XRL.World;
using XRL.World.Text;
using static QudUX.Concepts.Constants.MethodsAndFields;

namespace QudUX.HarmonyPatches
{
    /// <summary>
    /// Adds a "mark in journal" hotkey for legendary creatures to the Look UI.
    ///  1. After the hotkey bar text is built (TextBuilder.ToString() just after the debug-only
    ///     "show navweight" entry), pass it through LookExtender.ReturnModifiedString.
    ///  2. Where the Look loop decides whether to exit (key == NumPad5 or Escape sets the exit flag), let
    ///     LookExtender.CheckKeyPress handle the mark key and update the exit flag.
    /// Anchors are a string literal and typed locals rather than instruction offsets, so the patch
    /// survives the surrounding UI code being reshuffled.
    /// </summary>
    [HarmonyPatch(typeof(XRL.UI.Look))]
    public class Patch_XRL_UI_Look
    {
        [HarmonyTranspiler, HarmonyPatch("ShowLooker")]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            bool patched = TryPatch(codes);
            if (patched)
            {
                PatchHelpers.LogPatchResult("Look",
                    "Patched successfully." /* Adds option to mark legendary creature locations in the journal from the Look UI. */ );
                return codes;
            }
            PatchHelpers.LogPatchResult("Look",
                "Failed. This patch may not be compatible with the current game version. "
                + "The option to mark legendary creature locations in journal won't be available from the Look UI.");
            return instructions;
        }

        private static bool TryPatch(List<CodeInstruction> source)
        {
            var matcher = new CodeMatcher(source);

            // The looked-at object: the GameObject local tested just before the " | {{hotkey|" + Accept + "}} interact" entry.
            matcher.MatchStartForward(new CodeMatch(OpCodes.Ldstr, "}} interact"));
            if (matcher.IsInvalid) return false;
            object targetLocal = null;
            for (int i = matcher.Pos; i >= 0 && targetLocal == null; i--)
            {
                var ci = matcher.Instructions()[i];
                if (ci.IsLdloc() && ci.operand is LocalBuilder lb && lb.LocalType == typeof(GameObject))
                {
                    targetLocal = lb;
                }
            }
            if (targetLocal == null) return false;

            // 1. Hotkey bar: ... Append(" | {{hotkey|n}} show navweight") ... ldloc builder; callvirt ToString; stloc text
            matcher.MatchEndForward(
                new CodeMatch(OpCodes.Ldstr, " | {{hotkey|n}} show navweight"));
            if (matcher.IsInvalid) return false;
            matcher.MatchEndForward(
                new CodeMatch(ci => ci.IsLdloc() && ci.operand is LocalBuilder lb && lb.LocalType == typeof(TextBuilder)),
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(object), nameof(object.ToString))),
                new CodeMatch(ci => ci.IsStloc() && ci.operand is LocalBuilder lb && lb.LocalType == typeof(string)));
            if (matcher.IsInvalid) return false;
            object textLocal = matcher.Operand;
            matcher.Advance(1).Insert(
                new CodeInstruction(OpCodes.Ldloc_S, textLocal),
                new CodeInstruction(OpCodes.Ldloc_S, targetLocal),
                new CodeInstruction(OpCodes.Call, LookExtender_ReturnModifiedString),
                new CodeInstruction(OpCodes.Stloc_S, textLocal));

            // 2. Exit check: if (key == Keys.NumPad5 || key == Keys.Escape) flag = true;  [here]  ...next key test
            matcher.MatchStartForward(
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(OpCodes.Ldc_I4_S),
                new CodeMatch(ci => ci.opcode == OpCodes.Beq || ci.opcode == OpCodes.Beq_S),
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(OpCodes.Ldc_I4_S),
                new CodeMatch(ci => ci.opcode == OpCodes.Bne_Un || ci.opcode == OpCodes.Bne_Un_S),
                new CodeMatch(OpCodes.Ldc_I4_1),
                new CodeMatch(ci => ci.IsStloc()),
                new CodeMatch(ci => ci.IsLdloc()));
            while (matcher.IsValid && !(System.Convert.ToInt32(matcher.InstructionAt(1).operand) == (int)ConsoleLib.Console.Keys.NumPad5
                                        && System.Convert.ToInt32(matcher.InstructionAt(4).operand) == (int)ConsoleLib.Console.Keys.Escape))
            {
                matcher.Advance(1).MatchStartForward(
                    new CodeMatch(ci => ci.IsLdloc()), new CodeMatch(OpCodes.Ldc_I4_S),
                    new CodeMatch(ci => ci.opcode == OpCodes.Beq || ci.opcode == OpCodes.Beq_S),
                    new CodeMatch(ci => ci.IsLdloc()), new CodeMatch(OpCodes.Ldc_I4_S),
                    new CodeMatch(ci => ci.opcode == OpCodes.Bne_Un || ci.opcode == OpCodes.Bne_Un_S),
                    new CodeMatch(OpCodes.Ldc_I4_1), new CodeMatch(ci => ci.IsStloc()), new CodeMatch(ci => ci.IsLdloc()));
            }
            if (matcher.IsInvalid) return false;
            CodeInstruction loadKey = matcher.InstructionAt(0);
            CodeInstruction storeFlag = matcher.InstructionAt(7);
            CodeInstruction loadFlag = storeFlag.opcode == OpCodes.Stloc_0 ? new CodeInstruction(OpCodes.Ldloc_0)
                                     : storeFlag.opcode == OpCodes.Stloc_1 ? new CodeInstruction(OpCodes.Ldloc_1)
                                     : storeFlag.opcode == OpCodes.Stloc_2 ? new CodeInstruction(OpCodes.Ldloc_2)
                                     : storeFlag.opcode == OpCodes.Stloc_3 ? new CodeInstruction(OpCodes.Ldloc_3)
                                     : new CodeInstruction(OpCodes.Ldloc_S, storeFlag.operand);

            // The instruction after the flag store is the target of the bne.un above, so it carries labels:
            // move them onto our first inserted instruction so the "not E/Escape" path also runs our check.
            matcher.Advance(8);
            var first = new CodeInstruction(loadKey.opcode, loadKey.operand);
            first.MoveLabelsFrom(matcher.Instruction);
            matcher.Insert(
                first,
                new CodeInstruction(OpCodes.Ldloc_S, targetLocal),
                loadFlag,
                new CodeInstruction(OpCodes.Call, LookExtender_CheckKeyPress),
                new CodeInstruction(storeFlag.opcode, storeFlag.operand));

            source.Clear();
            source.AddRange(matcher.InstructionEnumeration());
            return true;
        }
    }
}
