using System.Linq;
using System.Reflection.Emit;
using System.Collections.Generic;
using ConsoleLib.Console;
using HarmonyLib;
using QudUX.ScreenExtenders;

namespace QudUX.HarmonyPatches
{
    /// <summary>
    /// Adds a "[M - Detailed Stats]" option to the classic High Scores screen (Scores.Show).
    ///  1. Right before the screen buffer is drawn each frame (the DrawBuffer call that follows the
    ///     "D / Del - Delete" hint), draw our own hint.
    ///  2. Right after the key is read (getvk -> stloc key), open the game stats screen on M.
    /// The drawing and key handling live in C# helpers so the injected IL stays a couple of calls.
    /// </summary>
    [HarmonyPatch(typeof(XRL.Core.Scores))]
    class Patch_XRL_Core_Scores
    {
        private static readonly System.Reflection.FieldInfo Scores_Buffer = AccessTools.Field(typeof(XRL.Core.Scores), "Buffer");
        private static readonly System.Reflection.FieldInfo Scores_Console = AccessTools.Field(typeof(XRL.Core.Scores), "Console");

        [HarmonyTranspiler]
        [HarmonyPatch("Show")]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            var drawBuffer = AccessTools.Method(typeof(TextConsole), nameof(TextConsole.DrawBuffer));
            var getvk = AccessTools.Method(typeof(Keyboard), nameof(Keyboard.getvk));

            var matcher = new CodeMatcher(codes)
                .MatchStartForward(new CodeMatch(ci => ci.opcode == OpCodes.Ldstr && ci.operand is string s && s.Contains("Del") && s.Contains("Delete")))
                .MatchStartForward(
                    new CodeMatch(OpCodes.Ldsfld, Scores_Console),
                    new CodeMatch(OpCodes.Ldsfld, Scores_Buffer),
                    new CodeMatch(OpCodes.Ldnull),
                    new CodeMatch(OpCodes.Ldc_I4_1),
                    new CodeMatch(OpCodes.Callvirt, drawBuffer),
                    new CodeMatch(), new CodeMatch(), new CodeMatch(),
                    new CodeMatch(OpCodes.Call, getvk),
                    new CodeMatch(ci => ci.IsStloc()));

            if (Scores_Buffer != null && Scores_Console != null && drawBuffer != null && getvk != null && matcher.IsValid)
            {
                CodeInstruction storeKey = matcher.InstructionAt(9);
                // The DrawBuffer sequence is a branch target (both hint branches join here): keep its labels on our call.
                var drawHint = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patch_XRL_Core_Scores), nameof(DrawDetailedStatsHint)));
                drawHint.MoveLabelsFrom(matcher.Instruction);
                matcher.Insert(drawHint);
                matcher.Advance(1 + 10).Insert(
                    new CodeInstruction(storeKey.opcode == OpCodes.Stloc_S || storeKey.opcode == OpCodes.Stloc ? OpCodes.Ldloc_S : LoadFor(storeKey.opcode), storeKey.operand),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patch_XRL_Core_Scores), nameof(HandleKey))));
                PatchHelpers.LogPatchResult("Scores",
                    "Patched successfully." /* Adds an option to open a detailed game stats menu from the High Scores console UI. */ );
                return matcher.InstructionEnumeration();
            }

            PatchHelpers.LogPatchResult("Scores",
                "Failed. This patch may not be compatible with the current game version. "
                + "The High Scores text UI won't have an option to open detailed stats.");
            return codes;
        }

        private static OpCode LoadFor(OpCode store)
        {
            if (store == OpCodes.Stloc_0) return OpCodes.Ldloc_0;
            if (store == OpCodes.Stloc_1) return OpCodes.Ldloc_1;
            if (store == OpCodes.Stloc_2) return OpCodes.Ldloc_2;
            return OpCodes.Ldloc_3;
        }

        public static void DrawDetailedStatsHint()
        {
            var buffer = Scores_Buffer.GetValue(null) as ScreenBuffer;
            buffer?.Goto(58, 23);
            buffer?.Write("&Y[&WM&y - Detailed Stats&Y]");
        }

        public static void HandleKey(Keys key)
        {
            if (key != Keys.M)
            {
                return;
            }
            EnhancedScoreboardExtender.ShowGameStatsScreen();
            // Redraw the high scores screen over the stats screen we just closed.
            var console = Scores_Console.GetValue(null) as TextConsole;
            var buffer = Scores_Buffer.GetValue(null) as ScreenBuffer;
            if (console != null && buffer != null)
            {
                DrawDetailedStatsHint();
                console.DrawBuffer(buffer, null, true);
            }
        }
    }
}
