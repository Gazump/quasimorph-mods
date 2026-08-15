using System;
using HarmonyLib;
using MGSC;
using SimpleJSON;

namespace LootTracker
{
    // Only a run id goes into the save. That is safe to leave behind because
    // LoadFromJSON.LoadFieldsAndProperties looks members up by name and never enumerates the
    // JSON, so an unknown key is never read and the save still loads without this mod.
    // A Components entry would not be: DeserializeGlobalComponents resolves types against
    // Assembly-CSharp only, and a mod type there ends up as Dictionary.Add(null, ...).
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
