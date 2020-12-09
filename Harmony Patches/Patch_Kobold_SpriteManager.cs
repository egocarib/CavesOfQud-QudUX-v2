using Kobold;
using HarmonyLib;
using QudUX.Concepts;
using QudUX.Utilities;

namespace QudUX.Harmony_Patches
{
    [HarmonyPatch(typeof(SpriteManager))]
    class Patch_Kobold_SpriteManager
    {
        [HarmonyPostfix]
        [HarmonyPatch("GetTextureInfo")]
        static void Postfix(string Path, ref exTextureInfo __result)
        {
            //This postfix assumes that it is always running on the UI (Unity) thread.
            if (__result == null && Path.EndsWith(Constants.FlippedTileSuffix))
            {
                if (TextureMaker.MakeFlippedTexture(Path, out exTextureInfo result))
                {
                    __result = result;
                }
            }
        }
    }
}
