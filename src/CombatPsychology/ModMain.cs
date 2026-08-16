using HarmonyLib;
using MGSC;
using UnityEngine;

namespace CombatPsychology
{
    public static class ModMain
    {
        private static string _localizationCache;

        // NOTE: the game's hook harvester double-registers when a mod declares two methods
        // for the same hook type, so keep exactly one method per ModHookType.

        [Hook(ModHookType.AfterConfigsLoaded)]
        public static void AfterConfigsLoaded(IModContext context)
        {
            IconFactory.ContentPath = context.ModContentPath;
            Registration.RegisterAll();
            new Harmony("quasimorph.combatpsychology").PatchAll(typeof(ModMain).Assembly);
            Debug.Log("[CombatPsychology] Loaded: stress, breakdowns, fortitude, treatments.");
        }

        [Hook(ModHookType.DungeonStarted)]
        public static void DungeonStarted(IModContext context)
        {
            RaidState.Reset();
            StressSystem.Difficulty = context.State.Get<Difficulty>();
        }

        [Hook(ModHookType.DungeonFinished)]
        public static void DungeonFinished(IModContext context)
        {
            StressSystem.Difficulty = null;
        }

        /// <summary>Appends this mod's rows to the game's tab-separated localization table.</summary>
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
