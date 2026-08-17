using HarmonyLib;
using MGSC;
using UnityEngine;

namespace Mechs
{
    public static class ModMain
    {
        private static string _localizationCache;

        [Hook(ModHookType.AfterConfigsLoaded)]
        public static void AfterConfigsLoaded(IModContext context)
        {
            IconFactory.ContentPath = context.ModContentPath;
            Registration.RegisterAll();
            new Harmony("quasimorph.mechs").PatchAll(typeof(ModMain).Assembly);
            Debug.Log("[Mechs] Loaded: Atlas exoframe.");
        }

        [Hook(ModHookType.DungeonStarted)]
        public static void DungeonStarted(IModContext context)
        {
            MechContext.Capture(context);
        }

        [Hook(ModHookType.AfterDungeonLoaded)]
        public static void AfterDungeonLoaded(IModContext context)
        {
            MechContext.Capture(context);
        }

        [Hook(ModHookType.DungeonFinished)]
        public static void DungeonFinished(IModContext context)
        {
            MechContext.Clear();
        }

        [Hook(ModHookType.ResourcesLoad)]
        public static Object LoadResource(string path)
        {
            if (path != "localization")
            {
                return null;
            }
            if (_localizationCache == null)
            {
                TextAsset textAsset = Resources.Load<TextAsset>("localization");
                if (textAsset == null)
                {
                    return null;
                }
                _localizationCache = textAsset.text + Registration.BuildLocalizationRows();
            }
            return new TextAsset(_localizationCache);
        }
    }
}
