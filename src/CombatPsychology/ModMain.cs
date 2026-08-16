using HarmonyLib;
using MGSC;
using UnityEngine;

namespace CombatPsychology
{
    public static class ModMain
    {
        private static string _localizationCache;

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
