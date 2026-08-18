using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MGSC;
using UnityEngine;

namespace RoguelikeMode
{
    public class RogueRunner : MonoBehaviour
    {
        private static RogueRunner _instance;

        private State _state;
        private DungeonGameMode _dungeon;

        public static RogueRunner Get(State state)
        {
            if (_instance == null)
            {
                GameObject holder = new GameObject("RoguelikeModeRunner");
                DontDestroyOnLoad(holder);
                _instance = holder.AddComponent<RogueRunner>();
            }
            _instance._state = state;
            return _instance;
        }

        public void BeginRun(int candidateIndex, RogueTier tier)
        {
            if (RogueRun.Active)
            {
                return;
            }
            if (string.IsNullOrEmpty(RogueRun.DayLabel))
            {
                RogueRun.PrepareDay(daily: true);
            }
            RogueRun.BeginRunState(candidateIndex, tier);
            CaptureModEnvironment();
            StartCoroutine(ProcessStart(null));
        }

        public void ResumeRun()
        {
            if (RogueRun.Active)
            {
                return;
            }
            RogueRunSave save = RunPersistence.LoadRun();
            if (save == null)
            {
                Debug.LogError("[RoguelikeMode] No valid run save to resume.");
                return;
            }
            RogueRun.PrepareDay(save.Daily);
            RogueRun.DaySeed = save.DaySeed;
            RogueRun.DayLabel = save.DayLabel;
            RogueRun.BeginRunState(save.CandidateIndex, (RogueTier)save.Tier);
            RogueRun.DeepestFloor = save.DeepestFloor;
            RogueRun.PlayerKills = save.PlayerKills;
            RogueRun.DamageTaken = save.DamageTaken;
            CaptureModEnvironment();
            StartCoroutine(ProcessStart(save));
        }

        private void CaptureModEnvironment()
        {
            RogueRun.ActiveMods.Clear();
            UserMods userMods = _state.Get<UserMods>();
            if (userMods == null)
            {
                return;
            }
            foreach (UserMod mod in userMods.Values)
            {
                if (!string.IsNullOrEmpty(mod.UniqueModName) && mod.UniqueModName != "RoguelikeMode")
                {
                    RogueRun.ActiveMods.Add(mod.UniqueModName);
                }
            }
            if (RogueRun.ActiveMods.Count > 0)
            {
                Debug.Log("[RoguelikeMode] Other mods active this run: " + string.Join(", ", RogueRun.ActiveMods));
            }
        }

        private IEnumerator ProcessStart(RogueRunSave resume)
        {
            yield return UI.Fade.ProcessFadeIn();
            GameModeStateMachine stateMachine = _state.Get<GameModeStateMachine>();
            MagnumProjects projects = _state.Get<MagnumProjects>();
            if (projects != null)
            {
                MagnumDevelopmentSystem.CleanCustomRecords(projects);
            }
            _state.Get<ComponentsLayout>().RemoveGlobalComponents();
            _state.Remove<SavedGameMetadata>();
            AccessTools.Method(typeof(GameModeStateMachine), "KillMainMenu").Invoke(stateMachine, null);
            SingletonMonoBehaviour<UI>.Instance.ReleaseViews();
            SingletonMonoBehaviour<ItemFactory>.Instance.SetConsumablesStackBonus(0);
            SingletonMonoBehaviour<ItemFactory>.Instance.SetWeaponDurabilityMult(1f);
            SingletonMonoBehaviour<ItemFactory>.Instance.SetArmorDurabilityMult(1f);
            _state.Resolve(new SavedGameMetadata(-1));
            _state.Get<ComponentsLayout>().CreateGlobalComponents();
            Difficulty difficulty = _state.Get<Difficulty>();
            difficulty.Preset = BuildRoguePreset();
            if (resume == null)
            {
                CreateRunMercenary();
            }
            else
            {
                AdoptResumedMercenary(resume.Merc);
            }
            Mission mission = MissionBuilder.BuildDailyMission(_state);
            if (mission == null)
            {
                yield return AbortToMenu("Roguelike mode failed to build the daily mission. Check Player.log.");
                yield break;
            }
            AuthorRaidMetadata(mission);
            RogueRun.Active = true;
            InputMapData firstFloor;
            if (resume != null)
            {
                _state.Get<RaidMetadata>().TurnNumber = resume.TurnNumber;
                int floor = RogueRun.FloorOf(resume.LocationId);
                firstFloor = new InputMapData
                {
                    locationId = resume.LocationId,
                    spawnIndex = Mathf.Max(0, floor - 1),
                    transitionType = TransitionType.Lifts
                };
                Debug.Log($"[RoguelikeMode] Resuming run {RogueRun.DayLabel} on {resume.LocationId}.");
            }
            else
            {
                firstFloor = new InputMapData
                {
                    locationId = mission.WorldStructure.StartLocation.ID
                };
                if (firstFloor.locationId == "arrival")
                {
                    GameLocation firstStage = mission.WorldStructure.FindNextLocation("arrival", TransitionType.Spaceships);
                    if (firstStage != null)
                    {
                        firstFloor.locationId = firstStage.ID;
                        firstFloor.transitionType = TransitionType.Spaceships;
                    }
                }
            }
            yield return LaunchFloor(firstFloor);
        }

        private DifficultyPreset BuildRoguePreset()
        {
            DifficultyPreset preset = CloneHelper.DeepClone(Data.DifficultyPresets[Data.Global.DefaultDifficulty]);
            preset.EvacRules = EvacRules.MissionOnly;
            preset.DeathPenalty = DeathPenalty.DieButMissionGone;
            preset.SmoothProgression = false;
            preset.MissionStageCountMod = 0f;
            if (RogueRun.Tier == RogueTier.Easy)
            {
                preset.EnemyDamageMult *= 0.8f;
                preset.EnemyHealth *= 0.85f;
                preset.MonsterPoints *= 0.85f;
                preset.ItemPoints *= 1.15f;
            }
            else if (RogueRun.Tier == RogueTier.Hard)
            {
                preset.EnemyDamageMult *= 1.2f;
                preset.EnemyHealth *= 1.15f;
                preset.MonsterPoints *= 1.25f;
                preset.ItemPoints *= 0.9f;
            }
            return preset;
        }

        private static int _candidatesSeed;
        private static readonly List<Mercenary> _candidateMercs = new List<Mercenary>();

        public static List<Mercenary> GetCandidateMercs(State state, bool forceRebuild = false)
        {
            if (!forceRebuild && _candidatesSeed == RogueRun.DaySeed && _candidateMercs.Count > 0)
            {
                return _candidateMercs;
            }
            _candidateMercs.Clear();
            _candidatesSeed = RogueRun.DaySeed;
            if (state.Get<Difficulty>() == null)
            {
                state.Resolve(Difficulty.Create());
            }
            PerkFactory perkFactory = state.Get<PerkFactory>();
            List<(string profileId, string classId)> pairs = GetDailyCandidates();
            for (int i = 0; i < pairs.Count; i++)
            {
                UnityEngine.Random.InitState(RogueRun.SeedFor("merc:" + i));
                MercenaryProfileRecord profile = Data.MercenaryProfiles.GetRecord(pairs[i].profileId);
                MercenaryClassRecord mercClass = Data.MercenaryClasses.GetRecord(pairs[i].classId);
                List<Perk> perks = new List<Perk>();
                Mercenary mercenary = MercenarySystem.GenerateMercenary(profile, mercClass.Id, perks);
                perks.Add(perkFactory.CreatePerk(Data.Perks.GetRecord(profile.TalentPerkId)));
                perks.Add(perkFactory.CreatePerk(Data.Perks.GetRecord("rank_0")));
                foreach (string perkId in mercClass.PerkIds)
                {
                    perks.Add(perkFactory.CreatePerk(Data.Perks.GetRecord(perkId)));
                }
                mercenary.SetMercClass(mercClass.Id, perks);
                _candidateMercs.Add(mercenary);
            }
            return _candidateMercs;
        }

        public static List<(string profileId, string classId)> GetDailyCandidates()
        {
            List<string> profiles = Data.MercenaryProfiles.Ids
                .Where(id => !id.EndsWith("_boss") && !id.EndsWith("_custom"))
                .OrderBy(id => id, System.StringComparer.Ordinal)
                .ToList();
            List<string> classes = Data.MercenaryClasses.Ids
                .Where(id => !id.EndsWith("_custom"))
                .OrderBy(id => id, System.StringComparer.Ordinal)
                .ToList();
            Shuffle(profiles, new System.Random(RogueRun.SeedFor("profiles")));
            Shuffle(classes, new System.Random(RogueRun.SeedFor("classes")));
            List<(string, string)> candidates = new List<(string, string)>();
            for (int i = 0; i < 3 && i < profiles.Count; i++)
            {
                candidates.Add((profiles[i], classes[i % classes.Count]));
            }
            return candidates;
        }

        private static void Shuffle<T>(List<T> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private void AdoptResumedMercenary(Mercenary mercenary)
        {
            Mercenaries mercenaries = _state.Get<Mercenaries>();
            MercenarySystem.SyncWithMagnumProgression(mercenary, _state.Get<MagnumProgression>());
            mercenaries.Values.Add(mercenary);
            mercenary.State = MercenaryState.InRaid;
            mercenary.EnabledProgression = true;
            Debug.Log($"[RoguelikeMode] Resumed mercenary: {mercenary.ProfileId} ({mercenary.MercClassId}).");
        }

        private void CreateRunMercenary()
        {
            List<Mercenary> candidates = GetCandidateMercs(_state, forceRebuild: true);
            Mercenary mercenary = candidates[Mathf.Clamp(RogueRun.CandidateIndex, 0, candidates.Count - 1)];
            Mercenaries mercenaries = _state.Get<Mercenaries>();
            MercenarySystem.SyncWithMagnumProgression(mercenary, _state.Get<MagnumProgression>());
            mercenaries.Values.Add(mercenary);
            mercenary.State = MercenaryState.InRaid;
            mercenary.EnabledProgression = true;
            Debug.Log($"[RoguelikeMode] Run mercenary: {mercenary.ProfileId} ({mercenary.MercClassId}), candidate {RogueRun.CandidateIndex}.");
        }

        private void AuthorRaidMetadata(Mission mission)
        {
            RaidMetadata raidMetadata = _state.Get<RaidMetadata>();
            Factions factions = _state.Get<Factions>();
            MagnumProgression magnumProgression = _state.Get<MagnumProgression>();
            Mercenaries mercenaries = _state.Get<Mercenaries>();
            raidMetadata.StationId = mission.StationId;
            raidMetadata.IsReversedMission = false;
            raidMetadata.CustomStage = null;
            raidMetadata.QMorphosMinLevel = factions.Get(mission.VictimFactionId).Record.MinQmorphosWhenVictims;
            raidMetadata.FreeGroupIndex = 0;
            raidMetadata.TurnNumber = 0;
            raidMetadata.QMorphosLevel = raidMetadata.QMorphosMinLevel;
            raidMetadata.QMorphosBaronSpawned = false;
            raidMetadata.IsBaronAllowed = true;
            raidMetadata.BlockedStages = new List<string>();
            raidMetadata.CustomCounters = new Dictionary<string, int>();
            raidMetadata.LoadLastSaveOnDeath = false;
            raidMetadata.EvacuationCompleted = false;
            raidMetadata.IsMaxQmorphosLevelOverriden = false;
            raidMetadata.OverridenMaxQmorphosLevel = 1000;
            raidMetadata.AchievementState.Reset();
            raidMetadata.BramfaturaId = mission.BramfaturaId;
            raidMetadata.WinCondition = mission.WinCondition;
            raidMetadata.WinCondition.ResetProcMissionParameters();
            raidMetadata.RaidType = RaidType.ProcMission;
            raidMetadata.World = mission.WorldStructure;
            magnumProgression.ResetDepartmentsForMissionStart();
            magnumProgression.GetDepartment<AutonomousCapsuleDepartment>().MarkItemsForbidEvacuate();
            magnumProgression.GetDepartment<ShuttleCargoDepartment>().MarkItemsForbidEvacuate();
            _state.Get<CombatLog>().Clear();
            Mercenary mercenary = mercenaries.MercenaryInRaid;
            mercenary.CreatureData.Health.ReasonOfDeath = HealthInfo.DeathReason.None;
            ItemInteractionSystem.MarkItemsForbidEvacuate(mercenary.CreatureData.Inventory);
        }

        private IEnumerator LaunchFloor(InputMapData inputMapData)
        {
            RogueRun.ResetAttempts(inputMapData.locationId);
            DungeonCreationResult result;
            _dungeon = _state.Get<GameModeFactory>().CreateDungeon(inputMapData, out result);
            if (result != DungeonCreationResult.Success)
            {
                Debug.LogError($"[RoguelikeMode] Dungeon creation failed ({result}) for {inputMapData.locationId}.");
                if (_dungeon != null)
                {
                    KillDungeon();
                }
                yield return AbortToMenu("Roguelike mode failed to generate the floor. Try again tomorrow or report this seed.");
                yield break;
            }
            _dungeon.OnFinished += DungeonFinished;
            StartCoroutine(_dungeon.Run());
        }

        public void JumpToFloor(int floor)
        {
            StartCoroutine(ProcessNextFloor(new DungeonFinishedData
            {
                Reason = GameFinishedReason.MoveNextLocation,
                To = new LocationAddress("stage" + floor, Mathf.Max(0, floor - 1)),
                TransitionType = TransitionType.Lifts
            }));
        }

        private void DungeonFinished(DungeonFinishedData data)
        {
            Debug.Log($"[RoguelikeMode] Floor finished, reason {data.Reason}, to '{data.To?.LocationUniqueId}'.");
            switch (data.Reason)
            {
                case GameFinishedReason.MoveNextLocation:
                    StartCoroutine(ProcessNextFloor(data));
                    break;
                case GameFinishedReason.RegenerateDungeon:
                    StartCoroutine(ProcessRegenerate());
                    break;
                default:
                    StartCoroutine(ProcessEndRun(data));
                    break;
            }
        }

        private IEnumerator ProcessNextFloor(DungeonFinishedData data)
        {
            yield return UI.Fade.ProcessFadeIn();
            KillDungeon();
            yield return new WaitForEndOfFrame();
            yield return LaunchFloor(new InputMapData
            {
                locationId = data.To.LocationUniqueId,
                spawnIndex = data.To.GatewayIndex,
                transitionType = data.TransitionType
            });
        }

        private IEnumerator ProcessRegenerate()
        {
            string locationId = _state.Get<LocationMetadata>().LocationId;
            yield return new WaitForEndOfFrame();
            KillDungeon();
            yield return new WaitForEndOfFrame();
            yield return LaunchFloor(new InputMapData
            {
                locationId = locationId
            });
        }

        private IEnumerator ProcessEndRun(DungeonFinishedData data)
        {
            RogueRun.Active = false;
            RunPersistence.Delete();
            bool victory = data.Reason == GameFinishedReason.MissionFinished;
            int turns = _state.Get<RaidMetadata>().TurnNumber;
            Mercenary mercenary = _state.Get<Mercenaries>().MercenaryInRaid;
            RogueScoreEntry entry = ScoreSystem.Record(mercenary?.ProfileId ?? "unknown", mercenary?.MercClassId ?? "unknown", turns, victory);
            string summary = BuildSummary(victory, data.Reason, turns, RogueRun.PlayerKills, entry);
            RogueRun.LastSummary = summary;
            yield return UI.Fade.ProcessFadeIn();
            KillDungeon();
            _state.Get<MonsterTransfer>()?.Clear();
            Mercenaries mercenaries = _state.Get<Mercenaries>();
            mercenaries.MutatedQuasimorph = null;
            mercenaries.ChangedMercenary = null;
            yield return new WaitForEndOfFrame();
            _state.Get<GameModeStateMachine>().RunMainMenu();
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            MenuInjection.OpenDive();
        }

        private string BuildSummary(bool victory, GameFinishedReason reason, int turns, int kills, RogueScoreEntry entry)
        {
            string headline = victory
                ? "THE GOLDEN KEYCARD IS YOURS. You conquered the descent."
                : (reason == GameFinishedReason.PlayerDead || reason == GameFinishedReason.GameOver)
                    ? "The descent claimed you."
                    : "Run abandoned.";
            string runLabel = RogueRun.Daily ? ("Daily run: " + RogueRun.DayLabel) : "Random run";
            string modsLine = (RogueRun.ActiveMods.Count > 0)
                ? $"\nOther mods active ({RogueRun.ActiveMods.Count}) - future ladder runs need RoguelikeMode only."
                : string.Empty;
            int best = ScoreSystem.BestScore();
            return $"{headline}\n\n{runLabel} ({RogueRun.Tier})\nDeepest floor: {RogueRun.DeepestFloor} / {RogueConfig.FloorCount}\nKills: {kills}\nDamage taken: {RogueRun.DamageTaken}\nTurns: {turns}\n\nSCORE: {entry.Score}{(entry.Score >= best ? "  (NEW BEST)" : $"  (best: {best})")}{modsLine}";
        }

        private IEnumerator AbortToMenu(string message)
        {
            RogueRun.Active = false;
            RunPersistence.Delete();
            RogueRun.LastSummary = message;
            yield return new WaitForEndOfFrame();
            _state.Get<GameModeStateMachine>().RunMainMenu();
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            MenuInjection.OpenDive();
        }

        private void KillDungeon()
        {
            if (_dungeon == null)
            {
                return;
            }
            Mercenaries mercenaries = _state.Get<Mercenaries>();
            mercenaries.MercenaryInRaid?.ClearDependencies();
            mercenaries.MutatedQuasimorph?.ClearDependencies();
            _dungeon.OnFinished -= DungeonFinished;
            _dungeon.ReleaseComponents();
            _dungeon.Kill();
            _dungeon = null;
        }
    }
}
