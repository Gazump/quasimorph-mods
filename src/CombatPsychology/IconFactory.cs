using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CombatPsychology
{
    /// <summary>Loads the mod's PNG icons into sprites, with a generated fallback so a
    /// missing file can never null-ref the game's UI.
    ///
    /// The game sizes icons from the sprite's pixelsPerUnit (SetNativeSize /
    /// SetNativeSizeX100), so every sprite is normalized against a sampled vanilla
    /// reference sprite: ppu = referencePpu * (ourWidth / referenceWidth) makes our icon
    /// render at exactly the reference's on-screen size.</summary>
    public static class IconFactory
    {
        private static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

        public static string ContentPath;

        /// <summary>A vanilla status-effect icon, sampled during registration; used to scale
        /// the effects-bar icons (status effects and this mod's buffs).</summary>
        public static Sprite StatusIconReference;

        public static Sprite Get(string spriteName)
        {
            return Get(spriteName, null);
        }

        public static Sprite Get(string spriteName, Sprite reference)
        {
            if (_cache.TryGetValue(spriteName, out Sprite cached))
            {
                return cached;
            }
            Texture2D texture = LoadTexture(spriteName) ?? CreateFallbackTexture();
            float pixelsPerUnit = 100f;
            if (reference != null && reference.rect.width > 0f)
            {
                pixelsPerUnit = reference.pixelsPerUnit * ((float)texture.width / reference.rect.width);
            }
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
            sprite.name = spriteName;
            _cache[spriteName] = sprite;
            return sprite;
        }

        private static Texture2D LoadTexture(string spriteName)
        {
            if (string.IsNullOrEmpty(ContentPath))
            {
                return null;
            }
            string path = Path.Combine(ContentPath, "icons", spriteName + ".png");
            if (!File.Exists(path))
            {
                Debug.LogWarning("[CombatPsychology] Missing icon file: " + path);
                return null;
            }
            Texture2D texture2D = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false);
            if (!texture2D.LoadImage(File.ReadAllBytes(path)))
            {
                Object.Destroy(texture2D);
                return null;
            }
            texture2D.filterMode = FilterMode.Point;
            return texture2D;
        }

        /// <summary>A flat magenta square: ugly on purpose, visible in-game, never null.</summary>
        private static Texture2D CreateFallbackTexture()
        {
            Texture2D texture2D = new Texture2D(16, 16, TextureFormat.RGBA32, mipChain: false);
            Color32[] pixels = new Color32[256];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(200, 60, 200, 255);
            }
            texture2D.SetPixels32(pixels);
            texture2D.Apply();
            texture2D.filterMode = FilterMode.Point;
            return texture2D;
        }
    }
}
