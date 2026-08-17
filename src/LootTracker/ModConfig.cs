using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace LootTracker
{
    public class ModConfig
    {
        public bool Enabled = true;

        public bool ShowBox = true;

        public float BoxThickness = 1f;

        public bool ShowGlyph = true;

        public bool ShowTechLevel = false;

        public string FallbackGlyph = "*";

        public string NeverOwnedColor = "#FFD24A";

        public string OwnedBeforeColor = "#FFFFFF";

        public string Corner = "TopRight";

        public float OffsetX = 1f;

        public float OffsetY = 1f;

        public float FontSize = 12f;

        public bool LogTechLevelHistogram = false;

        [JsonIgnore]
        public Color NeverOwned { get; private set; } = new Color(1f, 0.824f, 0.29f);

        [JsonIgnore]
        public Color OwnedBefore { get; private set; } = Color.white;

        [JsonIgnore]
        public Vector2 Anchor { get; private set; } = new Vector2(0f, 1f);

        public static ModConfig LoadOrCreate()
        {
            var config = new ModConfig();
            string path = Paths.ConfigFile;
            try
            {
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    if (!string.IsNullOrEmpty(json))
                    {
                        config = JsonConvert.DeserializeObject<ModConfig>(json) ?? new ModConfig();
                    }
                }
                else
                {
                    Directory.CreateDirectory(Paths.DataDirectory);
                    File.WriteAllText(path, JsonConvert.SerializeObject(config, Formatting.Indented));
                    Log.Info("Wrote a default config to " + path + ".");
                }
            }
            catch (Exception e)
            {
                Log.Warn("Failed to read config.json, using defaults. " + e.Message);
                config = new ModConfig();
            }

            config.Resolve();
            return config;
        }

        private void Resolve()
        {
            NeverOwned = ParseColor(NeverOwnedColor, new Color(1f, 0.824f, 0.29f));
            OwnedBefore = ParseColor(OwnedBeforeColor, Color.white);
            Anchor = ParseAnchor(Corner);
        }

        private static Color ParseColor(string html, Color fallback)
        {
            if (!string.IsNullOrEmpty(html) && ColorUtility.TryParseHtmlString(html, out Color parsed))
            {
                return parsed;
            }
            Log.Warn("Could not parse colour '" + html + "', using the default.");
            return fallback;
        }

        private static Vector2 ParseAnchor(string corner)
        {
            switch ((corner ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "topright": return new Vector2(1f, 1f);
                case "bottomleft": return new Vector2(0f, 0f);
                case "bottomright": return new Vector2(1f, 0f);
                case "topleft": return new Vector2(0f, 1f);
                default:
                    Log.Warn("Unknown Corner '" + corner + "', using TopLeft.");
                    return new Vector2(0f, 1f);
            }
        }
    }
}
