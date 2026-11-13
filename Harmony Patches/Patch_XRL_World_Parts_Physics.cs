using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using static QudUX.HarmonyPatches.PatchHelpers;
using static QudUX.Concepts.Constants.MethodsAndFields;

namespace QudUX.HarmonyPatches
{
    [HarmonyPatch(typeof(XRL.World.Parts.Physics))]
    class Patch_XRL_World_Parts_Physics
    {
        [HarmonyTranspiler]
        [HarmonyPatch("HandleEvent", new Type[] { typeof(XRL.World.ObjectEnteringCellEvent) })]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // Anchor: the "The way is blocked by ..." message pushed when the player bumps into a barrier.
            // Build 211 pushes the literal "The way is blocked by "; build 212+ (localized) pushes a key,
            // "Physics ObjectEnteringCellEvent PlayerBlocked", followed by "The way is blocked by =object.a.name=.".
            // Our call is stack-neutral, so it goes right before the message (before the key when there is one).
            var codes = instructions.ToList();
            bool patched = false;
            int at = codes.FindIndex(ci => ci.opcode == OpCodes.Ldstr && ci.operand is string s && s.StartsWith("The way is blocked by"));
            if (at >= 0)
            {
                if (at > 0 && codes[at - 1].opcode == OpCodes.Ldstr)
                {
                    at--; // localization key precedes the default text
                }
                var first = new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(codes[at]);
                codes.InsertRange(at, new[]
                {
                    first,
                    new CodeInstruction(OpCodes.Callvirt, IPart_get_ParentObject),
                    new CodeInstruction(OpCodes.Call, ParticleTextMaker_EmitFromPlayerIfBarrierInDifferentZone),
                });
                patched = true;
            }
            if (patched)
            {
                PatchHelpers.LogPatchResult("Physics.HandleEvent",
                    "Patched successfully." /* Adds option to show particle text messages when movement to connected zone is prevented. */ );
            }
            else
            {
                PatchHelpers.LogPatchResult("Physics.HandleEvent",
                    "Failed. This patch may not be compatible with the current game version. "
                    + "Some particle text effects may not be shown when movement is prevented.");
            }
            return codes;
        }
    }
}
