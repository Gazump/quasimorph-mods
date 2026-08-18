using System;
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
