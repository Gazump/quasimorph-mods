using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace LootTracker
{
    internal static class OwnedRegistry
    {
        private static readonly HashSet<string> EverOwned = new HashSet<string>(StringComparer.Ordinal);

        private static string _runId;

        public static bool WasEverOwned(string itemId)
        {
            return EverOwned.Contains(itemId);
        }

        public static void Clear()
        {
            EverOwned.Clear();
            _runId = null;
        }

        public static void NoteOwned(HashSet<string> ownedNow)
        {
            foreach (string id in ownedNow)
            {
                EverOwned.Add(id);
            }
        }

        public static string EnsureRunId()
        {
            if (string.IsNullOrEmpty(_runId))
            {
                _runId = Guid.NewGuid().ToString("N");
            }
            return _runId;
        }

        public static void AdoptRunId(string runId)
        {
            EverOwned.Clear();
            _runId = string.IsNullOrEmpty(runId) ? null : runId;
            if (_runId == null)
            {
                return;
            }

            string path = Paths.RegistryFile(_runId);
            try
            {
                if (!File.Exists(path))
                {
                    return;
                }
                var ids = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(path));
                if (ids == null)
                {
                    return;
                }
                for (int i = 0; i < ids.Count; i++)
                {
                    if (!string.IsNullOrEmpty(ids[i]))
                    {
                        EverOwned.Add(ids[i]);
                    }
                }
                Log.Info("Restored " + EverOwned.Count + " known item ids for run " + _runId + ".");
            }
            catch (Exception e)
            {
                Log.Warn("Could not read the registry for run " + _runId + ", starting empty. " + e.Message);
                EverOwned.Clear();
            }
        }

        public static void Write()
        {
            if (string.IsNullOrEmpty(_runId))
            {
                return;
            }
            string path = Paths.RegistryFile(_runId);
            try
            {
                Directory.CreateDirectory(Paths.DataDirectory);
                var ids = new List<string>(EverOwned);
                ids.Sort(StringComparer.Ordinal);
                File.WriteAllText(path, JsonConvert.SerializeObject(ids, Formatting.Indented));
            }
            catch (Exception e)
            {
                Log.Warn("Could not write the registry to " + path + ". " + e.Message);
            }
        }

        public static void MigrateFromLegacyFile(int slot)
        {
            if (slot < 0 || EverOwned.Count > 0)
            {
                return;
            }
            string path = Paths.LegacyRegistryFile(slot);
            try
            {
                if (!File.Exists(path))
                {
                    return;
                }
                var ids = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(path));
                if (ids == null)
                {
                    return;
                }
                for (int i = 0; i < ids.Count; i++)
                {
                    if (!string.IsNullOrEmpty(ids[i]))
                    {
                        EverOwned.Add(ids[i]);
                    }
                }
                Log.Info("Imported " + EverOwned.Count + " item ids from " + Path.GetFileName(path)
                    + ". That file can be deleted once this run has saved.");
            }
            catch (Exception e)
            {
                Log.Warn("Could not import the old registry file. " + e.Message);
            }
        }
    }
}
