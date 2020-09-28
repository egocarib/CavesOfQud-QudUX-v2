using System;
using HarmonyLib;
using XRL.World;
using XRL.World.Parts;
using QudUX.ScreenExtenders;

namespace QudUX.HarmonyPatches
{
    //[HarmonyPatch(typeof(Description))]
    class Patch_XRL_World_Parts_Description
    {
        //[HarmonyPrefix]
        //[HarmonyPatch("HandleEvent", new Type[] { typeof(InventoryActionEvent) })]
        //static void Prefix(Description __instance, InventoryActionEvent E)
        //{
        //    if (PopupExtender.DescriptionLookPopupActive = (E.Command == "Look"))
        //    {
        //        PopupExtender.CurrentLookObject = __instance.ParentObject;
        //    }
        //}

        //[HarmonyPostfix]
        //[HarmonyPatch("HandleEvent", new Type[] { typeof(InventoryActionEvent) })]
        //static void Postfix()
        //{
        //    PopupExtender.DescriptionLookPopupActive = false;
        //    PopupExtender.CurrentLookObject = null;
        //}
    }
}
