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

        private static bool IsString(CodeInstruction ci, string fragment) =>
            ci.opcode == OpCodes.Ldstr && ci.operand is string s && s.IndexOf(fragment, System.StringComparison.OrdinalIgnoreCase) >= 0;

        private static bool TryPatch(List<CodeInstruction> source)
        {
            var matcher = new CodeMatcher(source);

            // The looked-at object: the GameObject local tested just before the "interact" hotkey entry.
            // Build 211: ldstr "}} interact"; build 212+ (localized): ldstr "Look ShowLooker Interact".
            matcher.MatchStartForward(new CodeMatch(ci => IsString(ci, "interact")));
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

            // 1. Hotkey bar: ... the debug-only "show navweight" entry ... ldloc builder; callvirt ToString; stloc text
            matcher.MatchEndForward(new CodeMatch(ci => IsString(ci, "navweight")));
            if (matcher.IsInvalid) return false;
            matcher.MatchEndForward(
                new CodeMatch(ci => ci.IsLdloc() && ci.operand is LocalBuilder lb && lb.LocalType == typeof(TextBuilder)),
                new CodeMatch(ci => (ci.opcode == OpCodes.Callvirt || ci.opcode == OpCodes.Call)
                                    && ci.operand is System.Reflection.MethodInfo m && m.Name == nameof(object.ToString)
                                    && m.ReturnType == typeof(string) && m.GetParameters().Length == 0),
                new CodeMatch(ci => ci.IsStloc() && ci.operand is LocalBuilder lb && lb.LocalType == typeof(string)));
            if (matcher.IsInvalid) return false;
            object textLocal = matcher.Operand;
            matcher.Advance(1).Insert(
                new CodeInstruction(OpCodes.Ldloc_S, textLocal),
                new CodeInstruction(OpCodes.Ldloc_S, targetLocal),
                new CodeInstruction(OpCodes.Call, LookExtender_ReturnModifiedString),
                new CodeInstruction(OpCodes.Stloc_S, textLocal));

            // 2. Exit check: if (key == Keys.NumPad5 || key == Keys.Escape) <set exit flag>;  [here]  ...next key test
            //    Build 211 sets a local:            ldc.i4.1; stloc flag
            //    Build 212+ sets a field on state:  ldsfld Look.Looker; ldc.i4.1; stfld LookerState.Done
            var exitTest = new[]
            {
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(ci => ci.opcode == OpCodes.Ldc_I4_S && System.Convert.ToInt32(ci.operand) == (int)ConsoleLib.Console.Keys.NumPad5),
                new CodeMatch(ci => ci.opcode == OpCodes.Beq || ci.opcode == OpCodes.Beq_S),
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(ci => ci.opcode == OpCodes.Ldc_I4_S && System.Convert.ToInt32(ci.operand) == (int)ConsoleLib.Console.Keys.Escape),
                new CodeMatch(ci => ci.opcode == OpCodes.Bne_Un || ci.opcode == OpCodes.Bne_Un_S),
            };
            matcher.MatchStartForward(exitTest);
            if (matcher.IsInvalid) return false;
            CodeInstruction loadKey = matcher.InstructionAt(0);
            CodeInstruction a = matcher.InstructionAt(6), b = matcher.InstructionAt(7), c = matcher.InstructionAt(8);

            CodeInstruction[] loadFlag, storeFlagPrefix, storeFlag;
            int afterSet;
            if (a.opcode == OpCodes.Ldc_I4_1 && b.IsStloc())
            {
                loadFlag = new[] { LoadLocalFor(b) };
                storeFlagPrefix = new CodeInstruction[0];
                storeFlag = new[] { new CodeInstruction(b.opcode, b.operand) };
                afterSet = 8;
            }
            else if (a.opcode == OpCodes.Ldsfld && b.opcode == OpCodes.Ldc_I4_1 && c.opcode == OpCodes.Stfld && c.operand is System.Reflection.FieldInfo doneField)
            {
                loadFlag = new[] { new CodeInstruction(OpCodes.Ldsfld, a.operand), new CodeInstruction(OpCodes.Ldfld, doneField) };
                storeFlagPrefix = new[] { new CodeInstruction(OpCodes.Ldsfld, a.operand) };
                storeFlag = new[] { new CodeInstruction(OpCodes.Stfld, doneField) };
                afterSet = 9;
            }
            else
            {
                return false;
            }

            // The instruction after the flag store is the target of the bne.un above, so it carries labels:
            // move them onto our first inserted instruction so the "not NumPad5/Escape" path also runs our check.
            matcher.Advance(afterSet);
            var injected = new List<CodeInstruction>();
            injected.AddRange(storeFlagPrefix);
            injected.Add(new CodeInstruction(loadKey.opcode, loadKey.operand));
            injected.Add(new CodeInstruction(OpCodes.Ldloc_S, targetLocal));
            injected.AddRange(loadFlag);
            injected.Add(new CodeInstruction(OpCodes.Call, LookExtender_CheckKeyPress));
            injected.AddRange(storeFlag);
            injected[0].MoveLabelsFrom(matcher.Instruction);
            matcher.Insert(injected);

            source.Clear();
            source.AddRange(matcher.InstructionEnumeration());
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
    }
}
