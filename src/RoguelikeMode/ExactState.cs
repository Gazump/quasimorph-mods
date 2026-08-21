using System;
using MGSC;
using SimpleJSON;
using UnityEngine;

namespace RoguelikeMode
{
    public static class ExactState
    {
        private const string FileName = "roguelike_session.dat";

        public static bool HasFile()
        {
            FileManager fileManager = SingletonMonoBehaviour<FileManager>.Instance;
            return fileManager != null && fileManager.IsFileExist(FileName);
        }

        public static bool Save(State state)
        {
            try
            {
                LocationMetadata locationMetadata = state.Get<LocationMetadata>();
                JSONObject root = new JSONObject();
                root["SaveVersion"] = Data.Global.SaveVersion;
                root["LocationUniqueId"] = locationMetadata.LocationId;
                JSONObject global = new JSONObject();
                state.Get<ComponentsLayout>().SerializeGlobalComponents(global);
                root["Global"] = global;
                JSONObject dungeon = new JSONObject();
                JSONArray components = dungeon["Components"].AsArray;
                AddComponent<LocationMetadata>(state, components);
                AddComponent<MapMetadata>(state, components);
                AddComponent<MapGrid>(state, components);
                AddComponent<MapObstacles>(state, components);
                AddComponent<Visibilities>(state, components);
                AddComponent<MapEntities>(state, components);
                AddComponent<Creatures>(state, components);
                AddComponent<ItemsOnFloor>(state, components);
                AddComponent<FireController>(state, components);
                AddComponent<ToxicController>(state, components);
                AddComponent<GasController>(state, components);
                AddComponent<QmorphosController>(state, components);
                AddComponent<GibsController>(state, components);
                AddComponent<Scenarios>(state, components);
                root["Dungeon"] = dungeon;
                SingletonMonoBehaviour<FileManager>.Instance.SaveFile(FileName, root.ToString());
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError("[RoguelikeMode] Exact-state save failed: " + ex);
                Delete();
                return false;
            }
        }

        public static JSONNode Load()
        {
            FileManager fileManager = SingletonMonoBehaviour<FileManager>.Instance;
            if (fileManager == null || !fileManager.IsFileExist(FileName))
            {
                return null;
            }
            try
            {
                string text = fileManager.LoadTextFile(FileName);
                if (string.IsNullOrEmpty(text))
                {
                    return null;
                }
                JSONNode root = JSON.Parse(text);
                if (root["SaveVersion"].AsInt != Data.Global.SaveVersion)
                {
                    Debug.LogWarning("[RoguelikeMode] Exact-state save is from another game version, falling back to floor restart.");
                    Delete();
                    return null;
                }
                if (string.IsNullOrEmpty(root["LocationUniqueId"].Value))
                {
                    return null;
                }
                return root;
            }
            catch (Exception ex)
            {
                Debug.LogError("[RoguelikeMode] Exact-state load failed: " + ex);
                Delete();
                return null;
            }
        }

        public static void Delete()
        {
            FileManager fileManager = SingletonMonoBehaviour<FileManager>.Instance;
            if (fileManager != null && fileManager.IsFileExist(FileName))
            {
                fileManager.RemoveFile(FileName);
            }
        }

        private static void AddComponent<T>(State state, JSONArray components) where T : class
        {
            components.Add(SaveToJSON.CreateNode(state.Get<T>()));
        }
    }
}
