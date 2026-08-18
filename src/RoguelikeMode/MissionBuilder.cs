using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MGSC;
using UnityEngine;

namespace RoguelikeMode
{
    public static class MissionBuilder
    {
        private struct Theme
        {
            public ProcMissionTemplate Template;
            public string StationType;
        }

        public static Mission BuildDailyMission(State state)
        {
            Stations stations = state.Get<Stations>();
            Factions factions = state.Get<Factions>();
            SpaceTime spaceTime = state.Get<SpaceTime>();
            Difficulty difficulty = state.Get<Difficulty>();
            MissionFactory missionFactory = state.Get<MissionFactory>();
            List<Station> eligible = EligibleStations(stations, factions);
            if (eligible.Count == 0)
            {
                Debug.LogError("[RoguelikeMode] No eligible station for the daily mission.");
                return null;
            }
            System.Random rng = new System.Random(RogueRun.SeedFor("mission-pick"));
            Station chosen = eligible[rng.Next(eligible.Count)];
            Faction victim = factions.Get(chosen.OwnerFactionId);
            List<Faction> others = factions.Values.Where(f => f.Id != victim.Id && f.FactionType != FactionType.None).OrderBy(f => f.Id, StringComparer.Ordinal).ToList();
            Faction beneficiary = (others.Count > 0) ? others[rng.Next(others.Count)] : victim;
            Mission mission = new Mission
            {
                StationId = chosen.Id,
                BeneficiaryFactionId = beneficiary.Id,
                VictimFactionId = victim.Id,
                CreationTime = spaceTime.Time,
                ExpireTime = spaceTime.Time.AddYears(10),
                BramfaturaId = chosen.BramfaturaId,
                MissionDifficulty = RogueConfig.MissionDifficultyRating,
                ProcMissionType = ProceduralMissionType.Robbery,
                ProcSubTypeVariant = 1,
                StoryId = RogueConfig.StoryId,
                StagesNameId = chosen.Record.MissionNameTemplateId,
                MinTechLevel = 1,
                WorldStructure = new GameWorldStructure()
            };
            ProcMissionTemplate missionTemplate = Data.ProcMissionTemplates.GetRecord(chosen.Record.MissionTemplateId);
            UnityEngine.Random.InitState(RogueRun.SeedFor("mission-structure"));
            AccessTools.Method(typeof(MissionFactory), "GenerateProceduralStructure")
                .Invoke(missionFactory, new object[] { mission, missionTemplate, beneficiary, victim.Id, chosen.Record.StationType });
            string lastStageId = "stage" + RogueConfig.FloorCount;
            if (!mission.LocationPlans.ContainsKey(lastStageId))
            {
                Debug.LogError($"[RoguelikeMode] Generated mission has no {lastStageId}; stages: {string.Join(", ", mission.LocationPlans.Keys)}");
                return null;
            }
            ApplyFloorThemes(mission, missionFactory, eligible, missionTemplate, chosen.Record.StationType);
            ApplyEscalation(mission, difficulty);
            EnsureFloorNames(mission);
            mission.WinCondition.WinCondition = WinCondition.ItemInInventoryById;
            mission.WinCondition.WinConditionParameters.Clear();
            mission.WinCondition.WinConditionParameters.Add(RogueConfig.KeycardId);
            mission.WinCondition.WinConditionParameters.Add("1");
            mission.LocationPlans[lastStageId].AdditionalItemIdsDrop.Add(RogueConfig.KeycardId);
            Missions missions = state.Get<Missions>();
            missions.Values.RemoveAll(m => m.StationId == chosen.Id);
            missions.Values.Add(mission);
            Debug.Log($"[RoguelikeMode] Mission {RogueRun.DayLabel}: station {chosen.Id} ({chosen.Record.StationType}), victim {victim.Id}, beneficiary {beneficiary.Id}, {mission.LocationPlans.Count} plans.");
            return mission;
        }

        private static List<Station> EligibleStations(Stations stations, Factions factions)
        {
            List<Station> eligible = BuildEligible(stations, factions, requireRealFaction: true);
            if (eligible.Count == 0)
            {
                Debug.LogWarning("[RoguelikeMode] Station pool empty with faction filter; retrying without it.");
                eligible = BuildEligible(stations, factions, requireRealFaction: false);
            }
            return eligible;
        }

        private static List<Station> BuildEligible(Stations stations, Factions factions, bool requireRealFaction)
        {
            List<Station> eligible = new List<Station>();
            int noRecord = 0;
            int bramfaturan = 0;
            int noTemplate = 0;
            int badFaction = 0;
            foreach (Station station in stations.Values.OrderBy(s => s.Id, StringComparer.Ordinal))
            {
                if (string.IsNullOrEmpty(station.BramfaturaId) || string.IsNullOrEmpty(station.Record?.StationType) || string.IsNullOrEmpty(station.Record.MissionTemplateId))
                {
                    noRecord++;
                    continue;
                }
                if (station.Id.IndexOf("bramfatur", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    station.Record.StationType.IndexOf("bramfatur", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    bramfaturan++;
                    continue;
                }
                ProcMissionTemplate template = Data.ProcMissionTemplates.GetRecord(station.Record.MissionTemplateId);
                if (template == null || template.PresetFolders == null || template.PresetFolders.Count == 0)
                {
                    noTemplate++;
                    continue;
                }
                Faction owner = factions.Get(station.OwnerFactionId, logMissing: false);
                if (owner == null || (requireRealFaction && owner.FactionType == FactionType.None))
                {
                    badFaction++;
                    continue;
                }
                eligible.Add(station);
            }
            Debug.Log($"[RoguelikeMode] Station pool: {eligible.Count} eligible ({noRecord} no record, {bramfaturan} bramfaturan, {noTemplate} no template, {badFaction} bad faction, faction filter {requireRealFaction}).");
            return eligible;
        }

        private static void ApplyFloorThemes(Mission mission, MissionFactory missionFactory, List<Station> eligible, ProcMissionTemplate baseTemplate, string baseStationType)
        {
            List<Theme> themes = new List<Theme>();
            HashSet<string> seenTemplates = new HashSet<string>();
            foreach (Station station in eligible)
            {
                if (seenTemplates.Add(station.Record.MissionTemplateId))
                {
                    themes.Add(new Theme
                    {
                        Template = Data.ProcMissionTemplates.GetRecord(station.Record.MissionTemplateId),
                        StationType = station.Record.StationType
                    });
                }
            }
            if (themes.Count < 2)
            {
                return;
            }
            System.Random rng = new System.Random(RogueRun.SeedFor("themes"));
            var generatePlan = AccessTools.Method(typeof(MissionFactory), "GenerateProceduralPlan");
            Theme previous = new Theme { Template = baseTemplate, StationType = baseStationType };
            for (int floor = 2; floor <= RogueConfig.FloorCount; floor++)
            {
                Theme theme = themes[rng.Next(themes.Count)];
                if (theme.Template == previous.Template)
                {
                    theme = themes[rng.Next(themes.Count)];
                }
                string stageId = "stage" + floor;
                if (!mission.LocationPlans.ContainsKey(stageId))
                {
                    continue;
                }
                ProcMissionTemplate template = theme.Template;
                bool hasAlt = template.PresetFolders.Count > 1;
                BinaryPresetsMap.Presets presets = Data.PresetsMap.GetPresets(template.PresetFolders[hasAlt ? 1 : 0]);
                List<string> surface = hasAlt ? template.SurfaceTilsetsAlt : template.SurfaceTilsets;
                List<string> corner = hasAlt ? template.CornerWallTilesetsAlt : template.CornerWallTilesets;
                PresetRuleType fillType = hasAlt ? template.FillTypeAlt : template.FillType;
                List<string> environment = hasAlt ? template.FloorEnvironmentAlt : template.FloorEnvironment;
                bool fillOutdoor = hasAlt ? template.FillEnvironmentAltIsOutdoor : template.FillEnvironmentIsOutdoor;
                DungeonGenerationPlan plan = generatePlan.Invoke(missionFactory,
                    new object[] { mission, template, presets, false, surface, corner, fillType, false }) as DungeonGenerationPlan;
                plan.CustomParameters.Add("FillIsOutdoor", fillOutdoor.ToString());
                plan.EnvironmentPresets.Clear();
                plan.EnvironmentPresets.AddRange(environment);
                plan.MonstersTableIds.Add(theme.StationType);
                plan.ItemsTableIds.Add(mission.VictimFactionId);
                mission.LocationPlans[stageId] = plan;
                previous = theme;
                Debug.Log($"[RoguelikeMode] {stageId} theme: {theme.StationType} ({template.Id})");
            }
        }

        private static void ApplyEscalation(Mission mission, Difficulty difficulty)
        {
            MissionDifficultyRecord low = Data.MissionDifficulty.Get(1);
            MissionDifficultyRecord high = Data.MissionDifficulty.Get(5);
            if (low == null || high == null)
            {
                return;
            }
            foreach (KeyValuePair<string, DungeonGenerationPlan> pair in mission.LocationPlans)
            {
                int floor = RogueRun.FloorOf(pair.Key);
                if (floor < 1)
                {
                    continue;
                }
                float t = (floor - 1) / (float)(RogueConfig.FloorCount - 1);
                DungeonGenerationPlan plan = pair.Value;
                float mapScale = Mathf.Lerp(RogueConfig.FirstFloorMapScale, 1f, t);
                plan.MapGridWidth = Mathf.Max(45, Mathf.RoundToInt(plan.MapGridWidth * mapScale));
                plan.MapGridHeight = Mathf.Max(32, Mathf.RoundToInt(plan.MapGridHeight * mapScale));
                plan.MonstersPointsLimit = Mathf.RoundToInt(Mathf.Lerp(low.MonsterPointsPerStage, high.MonsterPointsPerStage * RogueConfig.TopMonsterPointsMult, t) * difficulty.Preset.MonsterPoints);
                plan.ItemsPointsLimit = Mathf.RoundToInt(Mathf.Lerp(low.ItemPointsPerStage, high.ItemPointsPerStage * RogueConfig.TopItemPointsMult, t) * difficulty.Preset.ItemPoints);
                plan.AlliesPointsMult = 0f;
                plan.QuestGroupsCount = 0;
                PresetRule rooms = plan.GetRule(PresetRuleType.RoomsGroup);
                if (rooms != null)
                {
                    rooms.Count = Mathf.Max(2, Mathf.RoundToInt(Mathf.Lerp(RogueConfig.MinRoomsFirstFloor, RogueConfig.MaxRoomsLastFloor, t)));
                }
                PresetRule corridors = plan.GetRule(PresetRuleType.PresetCorridors);
                if (corridors != null)
                {
                    corridors.Count = Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(1f, 3f, t)));
                }
                Debug.Log($"[RoguelikeMode] {pair.Key}: grid {plan.MapGridWidth}x{plan.MapGridHeight}, monsters {plan.MonstersPointsLimit}, items {plan.ItemsPointsLimit}, rooms {rooms?.Count ?? -1}, corridors {corridors?.Count ?? -1}, tech {RogueConfig.TechLevelForFloor(floor)}");
            }
        }

        private static void EnsureFloorNames(Mission mission)
        {
            foreach (string stageId in mission.LocationPlans.Keys)
            {
                int floor = RogueRun.FloorOf(stageId);
                if (floor >= 1)
                {
                    LocalizationInjector.EnsureKey($"mission.{mission.StagesNameId}.{stageId}.name", "FLOOR " + floor);
                }
            }
        }
    }
}
