using System;
using System.Collections.Generic;
using UnityEngine;
using QudUX.Concepts;
using static QudUX.Utilities.Logger;

namespace QudUX.Utilities
{
    class TextureMaker
    {
        public static Dictionary<string, exTextureInfo> SpriteManagerInfoMap => Constants.SpriteManagerInfoMap;

        public static bool MakeFlippedTexture(string flippedPath, out exTextureInfo generatedTextureInfo)
        {
            //this method assumes that:
            //  • You are calling it on the UI (Unity) thread
            //  • You are providing a path that ends with the qudux flipped suffix (i.e. _qudux_flipped.png)
            //  • SpriteManager.InfoMap is already initialized (generally because you should call
            //    SpriteManager.GetTextureInfo before calling this function)

            string sourcePath = flippedPath.Substring(0, flippedPath.LastIndexOf(Constants.FlippedTileSuffix));
            generatedTextureInfo = null;
            try
            {
                exTextureInfo info = GetTextureInfoFromBaseTilePath(sourcePath, out _);
                Texture2D texture2D = new Texture2D(info.width, info.height, TextureFormat.ARGB32, mipChain: false);
                texture2D.filterMode = UnityEngine.FilterMode.Point;
                Color[] pixels = info.texture.GetPixels(info.x, info.y, info.width, info.height, 0);
                //flip the tile pixels horizontally
                for (int row = 0; row < info.height; ++row)
                {
                    Array.Reverse(pixels, row * info.width, info.width);
                }
                texture2D.SetPixels(pixels);
                texture2D.Apply();
                // store texture in current game session's SpriteManager InfoMap
                string f = flippedPath.ToLower();
                if (f.Contains("textures"))
                {
                    f = "assets_content_" + f.Substring(f.IndexOf("textures"));
                }
                f = f.Replace('/', '_').Replace('\\', '_');
                exTextureInfo flippedTextureInfo = new exTextureInfo
                {
                    texture = texture2D,
                    width = texture2D.width,
                    height = texture2D.height,
                    x = 0,
                    y = 0,
                    ShaderMode = 0
                };
                if (SpriteManagerInfoMap == null)
                {
                    Log("QudUX: (Error) Couldn't MakeFlippedTexture because SpriteManager.InfoMap is not yet initialized.");
                    return false;
                }
                if (SpriteManagerInfoMap.ContainsKey(f))
                {
                    SpriteManagerInfoMap[f] = flippedTextureInfo;
                }
                else
                {
                    SpriteManagerInfoMap.Add(f, flippedTextureInfo);
                }
                string key = f.ToLower().Replace(".png", ".bmp");
                if (SpriteManagerInfoMap.ContainsKey(key))
                {
                    SpriteManagerInfoMap[key] = flippedTextureInfo;
                }
                else
                {
                    SpriteManagerInfoMap.Add(key, flippedTextureInfo);
                }
                key = f.ToLower().Replace(".png", "");
                if (SpriteManagerInfoMap.ContainsKey(key))
                {
                    SpriteManagerInfoMap[key] = flippedTextureInfo;
                }
                else
                {
                    SpriteManagerInfoMap.Add(key, flippedTextureInfo);
                }
                generatedTextureInfo = flippedTextureInfo;
                return true;
            }
            catch (Exception ex)
            {
                Log($"QudUX: (Error) Failed to generate flipped tile.\nException details: \n" + ex.ToString());
                return false;
            }
        }

        public static exTextureInfo GetTextureInfoFromBaseTilePath(string baseTilePath, out string originalTilePath)
        {
            string path = System.IO.Path.ChangeExtension(baseTilePath, null); //strip extension, if any
            string bmp = $"{path}.bmp".ToLower().Replace('/', '_').Replace('\\', '_');
            string png = $"{path}.png".ToLower().Replace('/', '_').Replace('\\', '_');
            exTextureInfo textureInfo;
            try
            {
                textureInfo = Resources.Load("TextureInfo/assets_content_textures_" + bmp) as exTextureInfo;
                if (textureInfo != null)
                {
                    originalTilePath = $"assets_content_textures_{bmp}";
                    return textureInfo;
                }
                textureInfo = Resources.Load("TextureInfo/" + bmp) as exTextureInfo;
                if (textureInfo != null)
                {
                    originalTilePath = bmp;
                    return textureInfo;
                }
                textureInfo = Resources.Load("TextureInfo/assets_content_textures_" + png) as exTextureInfo;
                if (textureInfo != null)
                {
                    originalTilePath = $"assets_content_textures_{png}";
                    return textureInfo;
                }
                textureInfo = Resources.Load("TextureInfo/" + png) as exTextureInfo;
                if (textureInfo != null)
                {
                    originalTilePath = png;
                    return textureInfo;
                }
                textureInfo = Resources.Load("TextureInfo/" + baseTilePath) as exTextureInfo;
                if (textureInfo != null)
                {
                    originalTilePath = baseTilePath;
                    return textureInfo;
                }
                //modded textures can't be loaded from Unity Resources obviously - but they should be in the InfoMap already:
                if (SpriteManagerInfoMap.TryGetValue($"assets_content_textures_{png}", out textureInfo))
                {
                    originalTilePath = $"assets_content_textures_{png}";
                    return textureInfo;
                }
                if (SpriteManagerInfoMap.TryGetValue($"assets_content_{png.Substring(png.IndexOf("textures"))}", out textureInfo))
                {
                    originalTilePath = $"assets_content_{png.Substring(png.IndexOf("textures"))}";
                    return textureInfo;
                }
            }
            catch (Exception ex)
            {
                Log("(Error) Issue attempting to retrieve texture info for an "
                    + "about-to-be-flipped tile.\nException details: \n" + ex.ToString());
            }
            originalTilePath = null;
            return null;
        }

        public static void UnflipGameObjectTexture(XRL.World.GameObject obj)
        {
            if (obj.pRender?.Tile != null && obj.pRender.Tile.EndsWith(Constants.FlippedTileSuffix))
            {
                string sourcePath = obj.pRender.Tile.Substring(0, obj.pRender.Tile.LastIndexOf(Constants.FlippedTileSuffix));
                GetTextureInfoFromBaseTilePath(sourcePath, out string originalTilePath);
                if (originalTilePath != null)
                {
                    obj.pRender.Tile = originalTilePath;
                }
            }
        }
    }
}
