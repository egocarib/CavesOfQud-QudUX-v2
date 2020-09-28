using HarmonyLib;
using QudUX.Concepts;

namespace QudUX.HarmonyPatches
{
    [HarmonyPatch(typeof(XRL.World.GameObject))]
    class Patch_XRL_World_GameObject_ShouldAutoget
    {
        [HarmonyPostfix]
        [HarmonyPatch("ShouldAutoget")]
        static void Postfix(XRL.World.GameObject __instance, ref bool __result)
        {
            if (__result == true && Options.UI.EnableAutogetExclusions)
            {
                __result = !XRL.World.Parts.QudUX_AutogetHelper.IsAutogetDisabledByQudUX(__instance);
            }
        }
    }
}
