using System;
using System.Collections.Generic;
using System.Globalization;
using MGSC;

namespace LootTracker
{
    internal static class Marker
    {
        private static readonly Dictionary<string, string> GlyphCache = new Dictionary<string, string>(StringComparer.Ordinal);

        private static bool _disabledByError;

        public static void Apply(ItemSlot slot, BasePickupItem item)
        {
            if (_disabledByError || slot == null)
            {
                return;
            }

            try
            {
                if (item == null || item.IsImplicit || string.IsNullOrEmpty(item.Id) || OwnershipIndex.HasNow(item.Id))
                {
                    slot.GetComponent<MarkerView>()?.Hide();
                    return;
                }

                ModConfig config = ModMain.Config;
                var view = slot.GetComponent<MarkerView>() ?? slot.gameObject.AddComponent<MarkerView>();
                view.Show(
                    GlyphFor(item, config),
                    OwnedRegistry.WasEverOwned(item.Id) ? config.OwnedBefore : config.NeverOwned);
            }
            catch (Exception e)
            {
                _disabledByError = true;
                Log.Error("Marker failed, disabling it for this session to avoid log spam. " + e);
            }
        }

        public static void Reset()
        {
            GlyphCache.Clear();
        }

        private static string GlyphFor(BasePickupItem item, ModConfig config)
        {
            if (!config.ShowTechLevel)
            {
                return config.FallbackGlyph;
            }

            if (GlyphCache.TryGetValue(item.Id, out string cached))
            {
                return cached;
            }

            string glyph = config.FallbackGlyph;
            ItemRecord record = item.Record<ItemRecord>();
            if (record != null && record.TechLevel > 0)
            {
                glyph = TechLevelGlyph(record.TechLevel, config);
            }

            GlyphCache[item.Id] = glyph;
            return glyph;
        }

        private static string TechLevelGlyph(int techLevel, ModConfig config)
        {
            if (techLevel >= 1 && techLevel <= 9)
            {
                return techLevel.ToString(CultureInfo.InvariantCulture);
            }
            switch (techLevel)
            {
                case 10: return "X";
                case 100: return "C";
                default:
                    Log.Warn("Unexpected TechLevel " + techLevel + "; falling back to '" + config.FallbackGlyph + "'.");
                    return config.FallbackGlyph;
            }
        }
    }
}
