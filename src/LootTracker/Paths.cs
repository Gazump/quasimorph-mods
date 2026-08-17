using System.IO;
using UnityEngine;

namespace LootTracker
{
    internal static class Paths
    {
        public static string DataDirectory
        {
            get { return Path.Combine(Application.persistentDataPath, "LootTracker"); }
        }

        public static string ConfigFile
        {
            get { return Path.Combine(DataDirectory, "config.json"); }
        }

        public static string RegistryFile(string runId)
        {
            return Path.Combine(DataDirectory, "run_" + runId + ".json");
        }

        public static string LegacyRegistryFile(int slot)
        {
            return Path.Combine(DataDirectory, "slot_" + slot + ".json");
        }
    }
}
