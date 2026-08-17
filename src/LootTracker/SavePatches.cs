using System;
using HarmonyLib;
using MGSC;
using SimpleJSON;

namespace LootTracker
{
    [HarmonyPatch(typeof(ComponentsLayout))]
    internal static class SavePatches
    {
        private const string RunIdKey = "LootTrackerRunId";

        [HarmonyPostfix]
        [HarmonyPatch(nameof(ComponentsLayout.SerializeGlobalComponents))]
        public static void AfterSerialize(JSONNode rootNode)
        {
            try
            {
                if (rootNode == null)
                {
                    return;
                }
                rootNode[RunIdKey] = new JSONString(OwnedRegistry.EnsureRunId());
                OwnedRegistry.Write();
            }
            catch (Exception e)
            {
                Log.Error("Failed to stamp the run id into the save; the save itself is unaffected. " + e);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(ComponentsLayout.DeserializeGlobalComponents))]
        public static void AfterDeserialize(JSONNode jsonNode)
        {
            try
            {
                JSONNode node = (jsonNode == null) ? null : jsonNode[RunIdKey];
                OwnedRegistry.AdoptRunId((node != null && node.IsString) ? node.Value : null);
            }
            catch (Exception e)
            {
                Log.Error("Failed to read the run id from the save; starting empty. " + e);
                OwnedRegistry.Clear();
            }
        }
    }
}
