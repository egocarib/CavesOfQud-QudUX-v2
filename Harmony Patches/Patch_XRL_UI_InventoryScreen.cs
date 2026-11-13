using HarmonyLib;
using XRL.Core;
using GameOptions = XRL.UI.Options;
using QudUXOptions = QudUX.Concepts.Options;

namespace QudUX.HarmonyPatches
{
    [HarmonyPatch(typeof(XRL.UI.InventoryScreen))]
    class Patch_XRL_UI_InventoryScreen
    {
        [HarmonyPrefix]
        [HarmonyPatch("Show")]
        static bool Prefix(XRL.World.GameObject GO, ref XRL.UI.ScreenReturn __result)
        {
            // The classic InventoryScreen is only reached when Options.ModernUI is off,
            // so the old OverlayPrereleaseInventory check is no longer needed.
            if (QudUXOptions.UI.UseQudUXInventory
                && XRLCore.Core.Game.Player.Body.GetConfusion() <= 0)
            {
                __result = new XRL.UI.QudUX_InventoryScreen().Show(GO);
                return false;
            }
            return true;
        }
    }
}