using System.Linq;
using System.Reflection.Emit;
using System.Collections.Generic;
using HarmonyLib;
using XRL.UI;
using static QudUX.Concepts.Constants.MethodsAndFields;

namespace QudUX.HarmonyPatches
{
    /// <summary>
    /// The following patch makes some modifications to the Manage Abilities screen (classic UI only).
    /// The main intent is to call our own custom function that improves the descriptions of many
    /// abilities or adds descriptions when the base game doesn't provide any.
    ///
    /// Vanilla builds the description panel as
    ///     TextBlock block = new TextBlock(nodes[index].Ability.Description, ...);
    /// and we inject, right after that store:
    ///     AbilityManagerExtender.UpdateAbilityText(nodes[index], block);
    /// The list, index and TextBlock locals are read from the matched code, so the patch does not
    /// depend on local slot numbers or on the TextBlock constructor's parameter list.
    /// </summary>
    [HarmonyPatch(typeof(XRL.UI.AbilityManager))]
    public class Patch_XRL_UI_AbilityManager
    {
        [HarmonyTranspiler]
        [HarmonyPatch("Show")]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            var matcher = new CodeMatcher(codes)
                .MatchStartForward(new CodeMatch(OpCodes.Ldstr, "You have no abilities to manage!"))
                .MatchStartForward(
                    new CodeMatch(ci => ci.IsLdloc()),                                     // nodes
                    new CodeMatch(ci => ci.IsLdloc()),                                     // index
                    new CodeMatch(OpCodes.Callvirt, List_AbilityNode_get_Item),
                    new CodeMatch(OpCodes.Ldfld, AbilityNode_Ability),
                    new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(XRL.World.Parts.ActivatedAbilityEntry), "Description")));

            if (matcher.IsValid)
            {
                CodeInstruction loadNodes = matcher.InstructionAt(0);
                CodeInstruction loadIndex = matcher.InstructionAt(1);
                matcher.MatchEndForward(
                    new CodeMatch(ci => ci.opcode == OpCodes.Newobj && ci.operand is System.Reflection.ConstructorInfo c && c.DeclaringType == typeof(TextBlock)),
                    new CodeMatch(ci => ci.IsStloc()));
                if (matcher.IsValid)
                {
                    object textBlockLocal = matcher.Operand;
                    matcher.Advance(1).Insert(
                        new CodeInstruction(loadNodes.opcode, loadNodes.operand),
                        new CodeInstruction(loadIndex.opcode, loadIndex.operand),
                        new CodeInstruction(OpCodes.Callvirt, List_AbilityNode_get_Item),
                        new CodeInstruction(OpCodes.Ldloc_S, textBlockLocal),
                        new CodeInstruction(OpCodes.Call, AbilityManagerExtender_UpdateAbilityText));
                    PatchHelpers.LogPatchResult("AbilityManager",
                        "Patched successfully." /* Improves activated ability descriptions and cooldown information on the Manage Abilities screen. */ );
                    return matcher.InstructionEnumeration();
                }
            }

            PatchHelpers.LogPatchResult("AbilityManager",
                "Failed. This patch may not be compatible with the current game version. Improved activated ability descriptions and cooldown information won't be added to the Manage Abilities screen.");
            return codes;
        }
    }
}
