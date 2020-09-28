using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using XRL;
using HarmonyLib;
using QudUX.Concepts;
using QudUX.Utilities;
using static QudUX.Utilities.Logger;
using static QudUX.Concepts.Constants.MethodsAndFields;

namespace QudUX.HarmonyPatches
{
    /// <summary>
    /// The following patch updates the ruin name generation function to remove "some forgotten ruins"
    /// as a possible ruin naming option, and also to ensure that all ruin names created during
    /// chargen are unique. Otherwise, duplicate ruin names tend to be very common.
    /// </summary>
    [HarmonyPatch]
    [HasGameBasedStaticCache]
    public class Patch_XRL_Annals_QudHistoryFactory
    {
        [GameBasedStaticCache] //ensure this gets reset for each new game
        public static HashSet<string> UsedRuinsNames = new HashSet<string>();

        private static bool TranspilePatchApplied = false;

        static Type QudHistoryFactoryType = AccessTools.TypeByName("XRL.Annals.QudHistoryFactory");

        /// <summary>
        /// Calculate target method. This is necessary because QudHistoryFactory is an internal type, so we
        /// can't simply use typeof() on it and put it in the HarmonyPatch attribute.
        /// </summary>
        [HarmonyTargetMethod]
        static MethodBase TargetMethod()
        {
            return QudHistoryFactoryType.GetMethod("NameRuinsSite", new Type[] { typeof(HistoryKit.History), typeof(bool).MakeByRefType() });
        }

        /// <summary>
        /// Postfix patch. Prevents duplicate ruins names from being generated - tries to generate a new name if
        /// the name has already been used. This is surprisingly common (especially after we remove "some
        /// forgotten ruins" from the picture). Ultimately this ensures all ruins names are unique.
        /// </summary>
        [HarmonyPostfix]
        static void Postfix(ref string __result)
        {
            if (!TranspilePatchApplied)
            {
                return;
            }
            int ct = 0;
            while (UsedRuinsNames.Contains(__result))
            {
                if (ct++ > 10)
                {
                    //In practice, I've never seen this take more than 2 attempts, but adding a short circuit just in case.
                    Log($"Failed to find a suitable name for Ruins location after >10 attempts. Allowing duplicate name: {__result}");
                    break;
                }
                __result = JournalUtilities.GenerateName();
            }
            UsedRuinsNames.Add(__result);
        }

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            //QudHistoryFactory.NameRuinsSite takes Stat.Random(0, 80) and then if the value is not less than
            //60, it returns "some forgotten ruins" as the name of a ruins site. To patch this function, we
            //will find the "Stat.Random(0, 80)" code, and replace it with "Stat.Random(0, 59)" which will
            //ensure that "some forgotten ruins" is never returned from the function.

            //Specifically, we need to find and overwrite the following sequence of IL instructions
            //	  IL_0321: ldc.i4.0
            //    IL_0322: ldc.i4.s  80
            //    IL_0324: call      int32 XRL.Rules.Stat::Random(int32, int32)

            var ins = new List<CodeInstruction>(instructions);

            //don't apply patch if option is toggled off
            if (!Options.Exploration.RenameRuins)
            {
                PatchHelpers.LogPatchResult("QudHistoryFactory",
                    "Skipped. The \"Force all ruins to have unique names\" option is disabled.");
                return ins.AsEnumerable();
            }

            for (var i = 0; i < ins.Count; i++)
            {
                if (ins[i].opcode == OpCodes.Ldc_I4_0 && (i + 2) < ins.Count)
                {
                    if (ins[i + 1].opcode == OpCodes.Ldc_I4_S && Convert.ToInt32(ins[i + 1].operand) == 80)
                    {
                        if (ins[i + 2].opcode == OpCodes.Call)
                        {
                            MethodInfo callMethodInfo = (MethodInfo)(ins[i + 2].operand);
                            if (callMethodInfo.MetadataToken == Stat_Random.MetadataToken)
                            {
                                TranspilePatchApplied = true;
                                PatchHelpers.LogPatchResult("QudHistoryFactory",
                                    "Patched successfully." /* Removes \"some forgotten ruins\" as a naming option and ensures all ruins have unique names. */ );
                                //We have found our target triplet of IL instructions
                                ins[i + 1].operand = 59; //make the modification
                                return ins.AsEnumerable(); //return the modified instructions
                            }
                        }
                    }
                }
            }
            PatchHelpers.LogPatchResult("QudHistoryFactory",
                "Failed. This patch may not be compatible with the current game version. Ruins may not all have unique names, and \"some forgotten ruins\" may still be used.");
            return ins.AsEnumerable();
        }
    }
}
