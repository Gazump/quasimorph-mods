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
            Mercenary mercenary = context.State.Get<Mercenaries>()?.MercenaryInRaid;
            if (mercenary != null)
            {
                TraumaSystem.ApplyAtRaidStart(mercenary);
            }
        }

        [Hook(ModHookType.DungeonFinished)]
        public static void DungeonFinished(IModContext context)
        {
            StressSystem.Difficulty = null;
        }

        /// <summary>Loading a mid-raid save skips DungeonStarted, so runtime state derived
        /// there is rebuilt here (RaidState's once-per-raid flags re-arm; acceptable).</summary>
        [Hook(ModHookType.AfterDungeonLoaded)]
        public static void AfterDungeonLoaded(IModContext context)
        {
            StressSystem.Difficulty = context.State.Get<Difficulty>();
            Mercenary mercenary = context.State.Get<Mercenaries>()?.MercenaryInRaid;
            if (mercenary != null && mercenary.CreatureData.EffectsController.HasAnyEffect<SurvivorsHighBuff>())
            {
                RaidState.SurvivorsHighActive = true;
            }
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
