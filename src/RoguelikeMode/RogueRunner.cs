using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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

        public void BeginRun(int candidateIndex, RogueTier tier, int totalFloors = 0)
        {
            if (RogueRun.Active)
            {
                return;
            }
            if (string.IsNullOrEmpty(RogueRun.DayLabel))
            {
                RogueRun.PrepareDay(daily: true);
            }
            RogueRun.BeginRunState(candidateIndex, tier, totalFloors);
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
            RogueRun.BeginRunState(save.CandidateIndex, (RogueTier)save.Tier, save.TotalFloors > 0 ? save.TotalFloors : RogueConfig.DefaultFloorCount);
            RogueRun.DeepestFloor = save.DeepestFloor;
            RogueRun.PlayerKills = save.PlayerKills;
            RogueRun.DamageTaken = save.DamageTaken;
            RogueRun.TradeCredit = save.TradeCredit;
            RogueRun.CheatsUsed = save.CheatsUsed;
            RogueRun.TerminalPosition = save.TerminalPosition ?? string.Empty;
            RogueRun.TerminalUsed = save.TerminalUsed;
            DateTime startedUtc;
            if (!string.IsNullOrEmpty(save.StartedUtc)
                && DateTime.TryParse(save.StartedUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out startedUtc))
            {
                RogueRun.StartedUtc = startedUtc;
            }
            CaptureModEnvironment();
            StartCoroutine(ProcessStart(save));
        }

        private void CaptureModEnvironment()
        {
            CaptureModEnvironment(_state);
        }

        public static void CaptureModEnvironment(State state)
        {
            RogueRun.ActiveMods.Clear();
            UserMods userMods = state.Get<UserMods>();
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
            SimpleJSON.JSONNode session = resume != null ? ExactState.Load() : null;
            if (session != null)
            {
                _state.Get<ComponentsLayout>().CreateGlobalComponents(initContent: false);
                _state.Get<ComponentsLayout>().DeserializeGlobalComponents(session["Global"]);
                _state.Get<Difficulty>().Preset = BuildRoguePreset();
                RaidMetadata restoredRaid = _state.Get<RaidMetadata>();
                restoredRaid.World = _state.Get<Missions>().Get(restoredRaid)?.WorldStructure;
                if (restoredRaid.World != null)
                {
                    RogueRun.Active = true;
                    RogueRun.ResumingExactState = true;
                    string exactLocation = session["LocationUniqueId"].Value;
                    ExactState.Delete();
                    Debug.Log($"[RoguelikeMode] Resuming exact state on {exactLocation}.");
                    yield return LaunchFloorFromSave(session["Dungeon"], new InputMapData
                    {
                        locationId = exactLocation,
                        transitionType = TransitionType.None
                    });
                    yield break;
                }
                Debug.LogError("[RoguelikeMode] Exact-state session had no mission world, rebuilding from floor entry.");
                ExactState.Delete();
                _state.Get<ComponentsLayout>().RemoveGlobalComponents();
                _state.Get<ComponentsLayout>().CreateGlobalComponents();
            }
            else
            {
                _state.Get<ComponentsLayout>().CreateGlobalComponents();
            }
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
            MissionDifficultyRecord rogueDifficulty = Data.MissionDifficulty.Get(RogueConfig.MissionDifficultyRating);
            if (rogueDifficulty != null)
            {
                rogueDifficulty.MinStages = RogueRun.TotalFloors;
                rogueDifficulty.MaxStages = RogueRun.TotalFloors;
            }
            Mission mission = MissionBuilder.BuildDailyMission(_state);
            if (mission == null)
            {
                yield return AbortToMenu("Roguelike mode failed to build the daily mission. Check Player.log.", keepSave: false);
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
            System.Exception failure;
            _dungeon = TryCreateDungeon(inputMapData, out result, out failure);
            if (failure != null)
            {
                Debug.LogError($"[RoguelikeMode] Floor generation threw for {inputMapData.locationId}:\n{failure}");
                if (_dungeon != null)
                {
                    KillDungeon();
                }
                string culprit = FindForeignMod(failure);
                string message = culprit != null
                    ? $"Floor generation crashed inside another mod ({culprit}).\nRun suspended - disable that mod, restart the game and RESUME."
                    : "Floor generation crashed.\nRun suspended - check Player.log, then RESUME.";
                yield return AbortToMenu(message, keepSave: true);
                yield break;
            }
            if (result != DungeonCreationResult.Success)
            {
                Debug.LogError($"[RoguelikeMode] Dungeon creation failed ({result}) for {inputMapData.locationId}.");
                if (_dungeon != null)
                {
                    KillDungeon();
                }
                yield return AbortToMenu("Roguelike mode failed to generate the floor. Try again tomorrow or report this seed.", keepSave: false);
                yield break;
            }
            _dungeon.OnFinished += DungeonFinished;
            StartCoroutine(_dungeon.Run());
        }

        private IEnumerator LaunchFloorFromSave(SimpleJSON.JSONNode dungeonNode, InputMapData inputMapData)
        {
            RogueRun.ResetAttempts(inputMapData.locationId);
            System.Exception failure = null;
            try
            {
                _state.Get<GameModeFactory>().CreateDungeon(dungeonNode, inputMapData);
                _dungeon = _state.Get<DungeonBuilder>().Result;
            }
            catch (System.Exception ex)
            {
                failure = ex;
                _dungeon = _state.Get<DungeonBuilder>()?.Result;
            }
            if (failure != null || _dungeon == null)
            {
                Debug.LogError($"[RoguelikeMode] Exact-state restore failed for {inputMapData.locationId}: {failure}");
                if (_dungeon != null)
                {
                    KillDungeon();
                }
                RogueRun.ResumingExactState = false;
                yield return AbortToMenu("Suspended run could not be restored exactly.\nRESUME will restart the floor from its entrance.", keepSave: true);
                yield break;
            }
            _dungeon.OnFinished += DungeonFinished;
            StartCoroutine(_dungeon.Run());
        }

        private DungeonGameMode TryCreateDungeon(InputMapData inputMapData, out DungeonCreationResult result, out System.Exception failure)
        {
            try
            {
                failure = null;
                return _state.Get<GameModeFactory>().CreateDungeon(inputMapData, out result);
            }
            catch (System.Exception ex)
            {
                failure = ex;
                result = DungeonCreationResult.GeometryFail;
                return _state.Get<DungeonBuilder>()?.Result;
            }
        }

        private string FindForeignMod(System.Exception failure)
        {
            string trace = failure.ToString();
            UserMods userMods = _state.Get<UserMods>();
            if (userMods != null)
            {
                foreach (UserMod mod in userMods.Values)
                {
                    if (!string.IsNullOrEmpty(mod.UniqueModName) && mod.UniqueModName != "RoguelikeMode" && trace.Contains(mod.UniqueModName))
                    {
                        return mod.UniqueModName;
                    }
                }
            }
            foreach (string line in trace.Split('\n'))
            {
                string frame = line.Trim();
                if (!frame.StartsWith("at ") || frame.StartsWith("at ("))
                {
                    continue;
                }
                int dot = frame.IndexOf('.', 3);
                if (dot <= 3)
                {
                    continue;
                }
                string root = frame.Substring(3, dot - 3);
                if (root != "MGSC" && root != "RoguelikeMode" && root != "UnityEngine" && root != "System" && root != "HarmonyLib" && root != "MonoMod")
                {
                    return root;
                }
            }
            return null;
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
                case GameFinishedReason.ExitToMainMenu:
                    StartCoroutine(ProcessSuspendRun());
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
            RogueScoreEntry entry = null;
            LadderClient.LastSubmitResult = string.Empty;
            if (!RogueRun.CheatsUsed)
            {
                entry = ScoreSystem.Record(mercenary?.ProfileId ?? "unknown", mercenary?.MercClassId ?? "unknown", turns, victory);
                LadderClient.SubmitRun(entry, RogueRun.ElapsedSeconds());
            }
            else
            {
                Debug.Log("[RoguelikeMode] Cheats were used this run - score not recorded.");
                LadderClient.LastSubmitResult = "cheats used - not ranked";
            }
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
                ? "THE GOLDEN KEYCARD IS YOURS. You conquered the Dive."
                : (reason == GameFinishedReason.PlayerDead || reason == GameFinishedReason.GameOver)
                    ? "The Dive claimed you."
                    : "Run abandoned.";
            string runLabel = RogueRun.Daily ? ("Daily run: " + RogueRun.DayLabel) : "Random run";
            string modsLine = (RogueRun.ActiveMods.Count > 0)
                ? $"\nOther mods active ({RogueRun.ActiveMods.Count}) - marked with * on the ladder."
                : string.Empty;
            string scoreLine;
            if (entry == null)
            {
                scoreLine = "SCORE: not recorded - cheats were used this run.";
            }
            else
            {
                int best = ScoreSystem.BestScore();
                scoreLine = $"SCORE: {entry.Score}{(entry.Score >= best ? "  (NEW BEST)" : $"  (best: {best})")}";
            }
            return $"{headline}\n\n{runLabel} ({RogueRun.Tier})\nDeepest floor: {RogueRun.DeepestFloor} / {RogueRun.TotalFloors}\nKills: {kills}\nDamage taken: {RogueRun.DamageTaken}\nTurns: {turns}\n\n{scoreLine}{modsLine}";
        }

        private IEnumerator ProcessSuspendRun()
        {
            RogueRun.Active = false;
            RunPersistence.SaveFloorEntry(_state);
            bool exact = ExactState.Save(_state);
            RogueRun.LastSummary = $"Run suspended on floor {RogueRun.CurrentFloor} ({(RogueRun.Daily ? ("daily " + RogueRun.DayLabel) : "random")}, {RogueRun.Tier}).\n{(exact ? "RESUME DIVE continues exactly where you left off." : "RESUME DIVE restarts the floor from its entrance.")}{(RogueRun.Daily ? "\nDaily saves expire when the daily resets." : string.Empty)}";
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

        private IEnumerator AbortToMenu(string message, bool keepSave)
        {
            RogueRun.Active = false;
            if (!keepSave)
            {
                RunPersistence.Delete();
            }
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
