using HarmonyLib;
using XRL;
using XRL.World;
using QudUX.Concepts;

namespace QudUX.HarmonyPatches
{
    /// <summary>
    /// The following patch updates the MagneticPulse mutation that is used (only) by pulsed field
    /// magnets so that it no longer rips items from the player's inventory or equipment. No other
    /// part of this mutation is altered - for example, it can still pull objects on the ground, other
    /// creatures' gear, or creatures wearing a lot of metal items toward itself.
    ///
    /// EmitMagneticPulse picks one metal item from each affected object and pulls it
    /// PullSquares(strength, PullResistance(item)) squares, skipping the item when that is <= 0.
    /// Reporting an overwhelming resistance for the player's own items makes vanilla skip them,
    /// without depending on the IL layout of EmitMagneticPulse (which was rewritten in 1.0 and
    /// broke the previous transpiler).
    /// </summary>
    [HarmonyPatch(typeof(XRL.World.Parts.Mutation.MagneticPulse))]
    public class Patch_XRL_World_Parts_Mutation_MagneticPulse
    {
        // Large enough that PullSquares = (strength - resistance) / 5 is always <= 0, small enough not to overflow.
        private const int ImmovableResistance = 1000000;

        [HarmonyPostfix]
        [HarmonyPatch("PullResistance")]
        static void Postfix(GameObject __0, ref int __result)
        {
            if (!Options.Exploration.DisableMagnets || __0 == null)
            {
                return;
            }
            GameObject player = The.Player;
            if (player != null && (__0.InInventory == player || __0.Equipped == player))
            {
                __result = ImmovableResistance;
            }
        }
    }
}
