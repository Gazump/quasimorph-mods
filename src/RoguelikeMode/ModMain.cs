using HarmonyLib;
using MGSC;
using UnityEngine;

namespace RoguelikeMode
{
    public static class ModMain
    {
        [Hook(ModHookType.AfterConfigsLoaded)]
        public static void AfterConfigsLoaded(IModContext context)
        {
            Registration.ContentPath = context.ModContentPath;
            Registration.RegisterAll();
            new Harmony(RogueConfig.HarmonyId).PatchAll(typeof(ModMain).Assembly);
            Debug.Log("[RoguelikeMode] Loaded: daily descent, " + RogueConfig.FloorCount + " floors.");
        }

        [Hook(ModHookType.MainMenuStarted)]
        public static void MainMenuStarted(IModContext context)
        {
            MenuInjection.Inject(context.State);
        }

        [Hook(ModHookType.DungeonStarted)]
        public static void DungeonStarted(IModContext context)
        {
            if (!RogueRun.Active)
            {
                return;
            }
            LocationMetadata locationMetadata = context.State.Get<LocationMetadata>();
            RaidMetadata raidMetadata = context.State.Get<RaidMetadata>();
            int floor = RogueRun.FloorOf(locationMetadata.LocationId);
            RogueRun.CurrentFloor = floor;
            if (floor > RogueRun.DeepestFloor)
            {
                RogueRun.DeepestFloor = floor;
            }
            for (int i = 1; i < floor; i++)
            {
                string stageId = "stage" + i;
                if (!raidMetadata.BlockedStages.Contains(stageId))
                {
                    raidMetadata.BlockedStages.Add(stageId);
                }
            }
            AchievementManager.SetRuntimeDisabled(disabled: true);
            SupplyCache.Spawn(context.State);
            Creatures creatures = context.State.Get<Creatures>();
            creatures.MonsterInjured += (victimData, hit, result) =>
            {
                if (RogueRun.Active && creatures.Player != null && victimData == creatures.Player.CreatureData && !hit.wasMiss && !hit.wasImmune && hit.finalDmg > 0)
                {
                    RogueRun.DamageTaken += hit.finalDmg;
                }
            };
            RunPersistence.SaveFloorEntry(context.State);
            Debug.Log($"[RoguelikeMode] Entered floor {floor}/{RogueConfig.FloorCount} ({locationMetadata.LocationId}), seed day {RogueRun.DayLabel}.");
        }

    }
}
