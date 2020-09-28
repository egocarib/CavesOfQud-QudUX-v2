using HarmonyLib;
using XRL.UI;
using XRL.World;
using XRL.World.Parts;
using GameOptions = XRL.UI.Options;
using QudUXOptions = QudUX.Concepts.Options;
using QudUXTradeUI = XRL.UI.QudUX_DummyTradeNamespace.TradeUI;

namespace QudUX.HarmonyPatches
{
    /// <summary>
    /// This patch is related to QudUX's trader dialog feature (ask when the trader will restock)
    /// </summary>
    [HarmonyPatch(typeof(TradeUI))]
    class Patch_XRL_UI_TradeUI
    {
        [HarmonyPrefix]
        [HarmonyPatch("ShowTradeScreen")]
        public static void Prefix(GameObject Trader)
        {
            QudUX_ConversationHelper.SetTraderInteraction(Trader);
        }
    }

    /// <summary>
    /// This patch is likely only temporary - it enables collapsible TradeUI sections. I have submitted this
    /// for inclusion in the base game, so if that happens, I'll remove this patch & feature from QudUX.
    /// </summary>
    [HarmonyPatch(typeof(TradeUI))]
    public static class Patch_XRL_UI_TradeUI_2
    {
        [HarmonyPrefix]
        [HarmonyPatch("ShowTradeScreen")]
        public static bool Prefix(GameObject Trader, float _costMultiple, TradeUI.TradeScreenMode screenMode)
        {
            if (GameOptions.OverlayPrereleaseTrade)
            {
                return true; //patch method not compatible with overlay trade UI
            }
            if (!QudUXOptions.UI.CollapsibleTradeUI)
            {
                return true; //user has disabled this feature
            }
            QudUXTradeUI.ShowTradeScreen(Trader, _costMultiple, (QudUXTradeUI.TradeScreenMode)screenMode);
            return false; //skip the original function
        }
    }
}
