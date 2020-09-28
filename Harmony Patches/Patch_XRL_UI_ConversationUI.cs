using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using HarmonyLib;
using XRL.Core;
using QudUX.Utilities;
using static QudUX.Concepts.Constants.MethodsAndFields;
using static QudUX.HarmonyPatches.PatchHelpers;

namespace QudUX.HarmonyPatches
{
    [HarmonyPatch]
    class Patch_XRL_UI_ConversationUI
    {
        //This is a more stable method of specifying the target method than using an attribute, because
        //this won't break if they modify the method signature, as long as the first parameter always
        //remains typeof(Conversation). Most often they just add new optional parameters if anything.
        [HarmonyTargetMethod]
        static MethodBase TargetMethod()
        {
            MethodBase ret = null;
            List<MethodInfo> methodsFromCoversationUI = AccessTools.GetDeclaredMethods(typeof(XRL.UI.ConversationUI));
            foreach (MethodInfo method in methodsFromCoversationUI)
            {
                if (method.Name == "HaveConversation")
                {
                    ret = method;
                    Type[] argumentTypes = method.GetGenericArguments();
                    if (argumentTypes.Length > 0 && argumentTypes[0] == typeof(XRL.World.Conversation))
                    {
                        return method;
                    }
                }
            }
            return ret;
        }

        [HarmonyPrefix]
        static void Prefix()
        {
            XRL.World.GameObject player = XRLCore.Core?.Game?.Player?.Body;
            if (player != null)
            {
                player.RequirePart<XRL.World.Parts.QudUX_ConversationHelper>();
            }
            ConsoleUtilities.SuppressScreenBufferImposters(true);
        }

        [HarmonyFinalizer]
        static void Finalizer()
        {
            ConsoleUtilities.SuppressScreenBufferImposters(false);
        }

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var Sequence1 = new PatchTargetInstructionSet(new List<PatchTargetInstruction>
            {
                new PatchTargetInstruction(OpCodes.Ldstr, " ]}}"),
                new PatchTargetInstruction(OpCodes.Callvirt, ScreenBuffer_Write, 2),
                new PatchTargetInstruction(OpCodes.Pop, 0)
            });

            bool patched = false;
            foreach (var instruction in instructions)
            {
                yield return instruction;
                if (!patched && Sequence1.IsMatchComplete(instruction))
                {
                    //draw conversation speaker's tile and temporarily suppress any Unity prefab animations beneath it
                    yield return new CodeInstruction(OpCodes.Ldsfld, ConversationUI__ScreenBuffer); //_ScreenBuffer
                    yield return new CodeInstruction(OpCodes.Ldarg_1); //Speaker
                    yield return new CodeInstruction(OpCodes.Call, ConversationUIExtender_DrawConversationSpeakerTile);
                    patched = true;
                }
            }
            if (patched)
            {
                PatchHelpers.LogPatchResult("ConversationUI",
                    "Patched successfully." /* Adds the speaker's sprite to the title bar of conversation windows. */ );
            }
            else
            {
                PatchHelpers.LogPatchResult("ConversationUI",
                    "Failed. This patch may not be compatible with the current game version. "
                    + "Sprites won't be added to the title bar of conversation windows.");
            }
        }
    }
}
