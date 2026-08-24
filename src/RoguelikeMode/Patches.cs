using System;
using System.Collections.Generic;
using HarmonyLib;
using MGSC;

namespace RoguelikeMode
{
    [HarmonyPatch(typeof(DungeonBuilder), "Create", new Type[] { typeof(InputMapData) })]
    public static class DungeonBuilderCreatePatch
    {
        public static void Prefix(InputMapData inputMapData)
        {
            if (RogueRun.Active)
            {
                RogueRun.ResetAttempts(inputMapData.locationId);
                UnityEngine.Random.InitState(RogueRun.SeedFor("floor:" + inputMapData.locationId));
            }
        }
    }

    [HarmonyPatch(typeof(ExordiumDungeonGenerator), "Generate")]
    public static class GeneratorSeedPatch
    {
        public static void Prefix()
        {
            if (RogueRun.Active)
            {
                UnityEngine.Random.InitState(RogueRun.NextAttemptSeed());
            }
        }
    }

    [HarmonyPatch(typeof(MissionSystem), "GetFactionEquipmentId",
        new Type[] { typeof(Factions), typeof(LocationMetadata), typeof(Mission), typeof(string), typeof(int) },
        new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Out })]
    public static class TechLevelByFloorPatch
    {
        public static void Postfix(LocationMetadata locationMetadata, ref int baseTechLevel)
        {
            Apply(locationMetadata?.LocationId, ref baseTechLevel);
        }

        internal static void Apply(string locationId, ref int baseTechLevel)
        {
            if (!RogueRun.Active)
            {
                return;
            }
            int floor = RogueRun.FloorOf(locationId);
            if (floor >= 1)
            {
                baseTechLevel = RogueConfig.TechLevelForFloor(floor);
            }
        }
    }

    [HarmonyPatch(typeof(MissionSystem), "GetFactionEquipmentId",
        new Type[] { typeof(Factions), typeof(string), typeof(Mission), typeof(string), typeof(int) },
        new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Out })]
    public static class TechLevelByFloorStringPatch
    {
        public static void Postfix(string locationId, ref int baseTechLevel)
        {
            TechLevelByFloorPatch.Apply(locationId, ref baseTechLevel);
        }
    }

    [HarmonyPatch(typeof(TutorialSystem), "ShowTutorialMessage")]
    public static class SuppressTutorialsPatch
    {
        public static bool Prefix()
        {
            return !RogueRun.Active;
        }
    }

    [HarmonyPatch(typeof(ConsoleDaemon), "ExecuteCommandInternal")]
    public static class CheatWatchPatch
    {
        private static readonly string[] AllowedCommands = { "help", "clear", "rogue_info", "rogue_scores", "rogue_start", "rogue_abort", "rogue_httptest" };

        public static void Postfix(ConsoleDaemon __instance, string commandString, ref string __result)
        {
            if (!RogueRun.Active || RogueRun.CheatsUsed || string.IsNullOrEmpty(commandString))
            {
                return;
            }
            string command = commandString.Trim().Split(' ')[0].ToLowerInvariant();
            if (command.Length == 0 || Array.IndexOf(AllowedCommands, command) >= 0)
            {
                return;
            }
            if (__result != null && __result.StartsWith("[Error]"))
            {
                return;
            }
            RogueRun.CheatsUsed = true;
            if (AccessTools.Field(typeof(ConsoleDaemon), "_state").GetValue(__instance) is State state)
            {
                RunPersistence.SaveFloorEntry(state);
            }
            __result += Environment.NewLine + "<color=red>The Dive: console command detected - this run will not be recorded.</color>";
            UnityEngine.Debug.Log($"[RoguelikeMode] Command '{command}' used mid-run - score recording disabled for this run.");
        }
    }

    [HarmonyPatch(typeof(ItemDropSystem), "PrepareItemDropRecords")]
    public static class CaseLootFilterPatch
    {
        public static void Prefix(List<ItemRecord> items)
        {
            if (!RogueRun.Active || RogueRun.Tier == RogueTier.Easy || items == null)
            {
                return;
            }
            items.RemoveAll(record => Array.IndexOf(RogueConfig.BlockedContainerIds, record.Id) >= 0);
        }
    }

    [HarmonyPatch(typeof(ElevatorWindow), "Configure")]
    public static class ElevatorWarningPatch
    {
        private const string Warning = "This elevator descends only. Once you go down, there is no coming back up.";

        public static void Postfix(ElevatorWindow __instance)
        {
            if (!RogueRun.Active)
            {
                return;
            }
            LocalizableLabel description = AccessTools.Field(typeof(ElevatorWindow), "_elevatorDesc").GetValue(__instance) as LocalizableLabel;
            if (description == null)
            {
                return;
            }
            string descTag = AccessTools.Field(typeof(ElevatorWindow), "_descTag").GetValue(__instance) as string;
            string baseText = string.IsNullOrEmpty(descTag) ? string.Empty : Localization.Get(descTag, warnIfMissingTag: false);
            if (baseText == descTag)
            {
                baseText = string.Empty;
            }
            string composed = string.IsNullOrEmpty(baseText) ? Warning : (baseText + "\n\n" + Warning);
            LocalizationInjector.Set("ui.dive.elevatordesc", composed);
            description.ChangeLabel("ui.dive.elevatordesc");
        }
    }

    [HarmonyPatch(typeof(AchievementProgress), "ProcessCreatureKilledByDamage")]
    public static class PlayerKillCounterPatch
    {
        public static void Postfix(Creature victim, Creature damageDealer)
        {
            if (RogueRun.Active && damageDealer is Player && !(victim is Player))
            {
                RogueRun.PlayerKills++;
            }
        }
    }
}
