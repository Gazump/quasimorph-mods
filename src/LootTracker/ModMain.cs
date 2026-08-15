using System;
using HarmonyLib;
using MGSC;

namespace LootTracker
{
    public static class ModMain
    {
        private const string HarmonyId = "quasimorph.loottracker";

        internal static ModConfig Config { get; private set; } = new ModConfig();

        [Hook(ModHookType.AfterConfigsLoaded)]
        public static void AfterConfigsLoaded(IModContext context)
        {
            try
            {
                Config = ModConfig.LoadOrCreate();
                if (!Config.Enabled)
                {
                    Log.Info("Disabled via config.json.");
                    return;
                }

                if (Config.LogTechLevelHistogram)
                {
                    TechLevelReport.Write();
                }

                new Harmony(HarmonyId).PatchAll(typeof(ModMain).Assembly);
                Log.Info("Ready.");
            }
            catch (Exception e)
            {
                Log.Error("Startup failed. " + e);
            }
        }

        [Hook(ModHookType.SpaceStarted)]
        public static void SpaceStarted(IModContext context)
        {
            BeginRun(context);
        }

        [Hook(ModHookType.DungeonStarted)]
        public static void DungeonStarted(IModContext context)
        {
            BeginRun(context);
        }

        [Hook(ModHookType.MainMenuStarted)]
        public static void MainMenuStarted(IModContext context)
        {
            OwnedRegistry.Clear();
            OwnershipIndex.Invalidate();
            Marker.Reset();
        }

        private static void BeginRun(IModContext context)
        {
            if (!Config.Enabled)
            {
                return;
            }
            try
            {
                OwnedRegistry.MigrateFromLegacyFile(context.State?.Get<SavedGameMetadata>()?.Slot ?? -1);
                OwnershipIndex.Invalidate();
            }
            catch (Exception e)
            {
                Log.Error("Failed to start tracking for this run. " + e);
            }
        }
    }
}
