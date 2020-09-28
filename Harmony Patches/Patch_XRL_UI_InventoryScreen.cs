using HarmonyLib;
using QudUX.Concepts;

namespace QudUX.HarmonyPatches
{
    [HarmonyPatch(typeof(XRL.UI.InventoryScreen))]
    class Patch_XRL_UI_InventoryScreen
    {
        [HarmonyPrefix]
        [HarmonyPatch("Show")]
        static bool Prefix(XRL.World.GameObject GO, ref XRL.UI.ScreenReturn __result)
        {
            if (Options.UI.UseQudUXInventory)
            {
                __result = (new XRL.UI.QudUX_InventoryScreen()).Show(GO);
                return false;
            }
            return true;
        }
    }
}