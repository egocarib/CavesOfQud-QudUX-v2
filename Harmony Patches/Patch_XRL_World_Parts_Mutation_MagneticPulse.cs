using System.Linq;
using System.Reflection.Emit;
using System.Collections.Generic;
using HarmonyLib;
using QudUX.Concepts;
using static QudUX.Concepts.Constants.MethodsAndFields;

namespace QudUX.HarmonyPatches
{
    /// <summary>
    /// The following patch updates the MagneticPulse mutation that is used (only) by pulsed field
    /// magnets so that it no longer rips items from the inventory. No other part of this mutation
    /// is altered - for example, it can still pull objects on the ground or creatures wearing a lot
    /// of metal items toward itself. Only the bit that rips away inventory items is prevented.
    /// </summary>
    [HarmonyPatch(typeof(XRL.World.Parts.Mutation.MagneticPulse))]
    public class Patch_XRL_World_Parts_Mutation_MagneticPulse
    {
        private static readonly CodeInstruction TargetInstruction = new CodeInstruction(OpCodes.Callvirt, GameObject_RemoveFromContext);

        [HarmonyTranspiler]
        [HarmonyPatch("EmitMagneticPulse")]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool patchComplete = false;
            var ilcodes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < ilcodes.Count; i++)
            {
                if (PatchHelpers.InstructionsAreEqual(ilcodes[i], TargetInstruction))
                {
                    if (ilcodes[i - 2].opcode == OpCodes.Br)
                    {
                        object jumpTarget = ilcodes[i - 2].operand;
                        List<CodeInstruction> optionSwitch = PatchHelpers.ILBlocks.IfOptionYesJumpTo(Options.Exploration.OptionStrings.DisableMagnets, jumpTarget);
                        
                        //clone and then null out this instruction, preserving the jump label here
                        CodeInstruction shiftedInstruction = ilcodes[i - 1].Clone();
                        ilcodes[i - 1].opcode = OpCodes.Nop;
                        ilcodes[i - 1].operand = null;

                        //insert our option checking code next after the Nop/label
                        ilcodes.InsertRange(i, optionSwitch);

                        //reinsert the cloned instruction we copied earlier (now without a label)
                        ilcodes.Insert(i + optionSwitch.Count, shiftedInstruction);

                        patchComplete = true;
                        break;
                    }
                }
            }
            if (patchComplete)
            {
                PatchHelpers.LogPatchResult("MagneticPulse",
                    "Patched successfully." /* Enables option to prevent pulsed field magnets from ripping items from your inventory. */ );
            }
            else
            {
                PatchHelpers.LogPatchResult("MagneticPulse",
                    "Failed. This patch may not be compatible with the current game version. "
                    + "The option to prevent pulsed field magnets from ripping items from your inventory won't work.");
            }
            return ilcodes.AsEnumerable();
        }
    }
}
