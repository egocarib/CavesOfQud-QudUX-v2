using HarmonyLib;
using static QudUX.Utilities.Logger;

namespace QudUX.HarmonyPatches
{
    /// <summary>
    /// This is a dummy patch that does nothing. It comes last alphabetically among all QudUX patch
    /// classes (Harmony seems to generally run the patches in alphabetical order by Class name).
    /// The purpose is to log all of the output from other patches in a single log statement.
    /// </summary>
    [HarmonyPatch]
    class Z_PatchLogFinalizer
    {
        [HarmonyCleanup]
        static void Cleanup()
        {
            FlushCompiledLog("Patches");
        }
    }
}
