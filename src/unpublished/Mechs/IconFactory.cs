using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Mechs
{
    public static class IconFactory
    {
        private static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

        public static string ContentPath;

        public static Sprite GetOrNull(string spriteName, Sprite reference)
        {
            if (_cache.TryGetValue(spriteName, out Sprite cached))
            {
                return cached;
            }
            Texture2D texture = LoadTexture(spriteName);
            if (texture == null)
            {
                return null;
            }
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
    }
}
