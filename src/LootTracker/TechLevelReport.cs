using System;
using System.Collections.Generic;
using System.Text;
using MGSC;

namespace LootTracker
{
    // Item configs live inside Unity's Resources bundle and cannot be read off disk, so the
    // only way to see how TechLevel is distributed is to ask the loaded game.
    internal static class TechLevelReport
    {
        public static void Write()
        {
            try
            {
                var counts = new SortedDictionary<int, int>();
                int total = 0;
                int withoutItemRecord = 0;

                foreach (BasePickupItemRecord record in Data.Items.Records)
                {
                    total++;
                    ItemRecord item = (record as CompositeItemRecord)?.GetRecord<ItemRecord>();
                    if (item == null)
                    {
                        withoutItemRecord++;
                        continue;
                    }
                    counts.TryGetValue(item.TechLevel, out int seen);
                    counts[item.TechLevel] = seen + 1;
                }

                var report = new StringBuilder();
                report.Append("TechLevel histogram over ").Append(total).Append(" item records");
                if (withoutItemRecord > 0)
                {
                    report.Append(" (").Append(withoutItemRecord).Append(" have no ItemRecord)");
                }
                report.AppendLine(":");
                foreach (KeyValuePair<int, int> entry in counts)
                {
                    report.Append("  TechLevel ").Append(entry.Key).Append(": ").Append(entry.Value).AppendLine(" items");
                }

                Log.Info(report.ToString());
            }
            catch (Exception e)
            {
                Log.Warn("TechLevel histogram failed. " + e.Message);
            }
        }
    }
}
