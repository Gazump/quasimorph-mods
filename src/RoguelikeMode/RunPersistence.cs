using System;
using MGSC;
using SimpleJSON;
using UnityEngine;

namespace RoguelikeMode
{
    public class RogueRunSave
    {
        [Save]
        public int DaySeed { get; set; }

        [Save]
        public string DayLabel { get; set; }

        [Save]
        public bool Daily { get; set; }

        [Save]
        public int Tier { get; set; }

        [Save]
        public int CandidateIndex { get; set; }

        [Save]
        public string LocationId { get; set; }

        [Save]
        public int DeepestFloor { get; set; }

        [Save]
        public int PlayerKills { get; set; }

        [Save]
        public int DamageTaken { get; set; }

        [Save]
        public int TurnNumber { get; set; }

        [Save]
        public int TradeCredit { get; set; }

        [Save]
        public bool CheatsUsed { get; set; }

        [Save]
        public string TerminalPosition { get; set; }

        [Save]
        public bool TerminalUsed { get; set; }

        [Save]
        public string StartedUtc { get; set; }

        [Save]
        public Mercenary Merc { get; set; }
    }

    public static class RunPersistence
    {
        private const string FileName = "roguelike_run.dat";

        public static bool HasSave()
        {
            return LoadRun() != null;
        }

        public static void SaveFloorEntry(State state)
        {
            try
            {
                Mercenary mercenary = state.Get<Mercenaries>()?.MercenaryInRaid;
                if (mercenary == null)
                {
                    return;
                }
                RogueRunSave save = new RogueRunSave
                {
                    DaySeed = RogueRun.DaySeed,
                    DayLabel = RogueRun.DayLabel,
                    Daily = RogueRun.Daily,
                    Tier = (int)RogueRun.Tier,
                    CandidateIndex = RogueRun.CandidateIndex,
                    LocationId = RogueRun.CurrentLocationId,
                    DeepestFloor = RogueRun.DeepestFloor,
                    PlayerKills = RogueRun.PlayerKills,
                    DamageTaken = RogueRun.DamageTaken,
                    TurnNumber = state.Get<RaidMetadata>()?.TurnNumber ?? 0,
                    TradeCredit = RogueRun.TradeCredit,
                    CheatsUsed = RogueRun.CheatsUsed,
                    TerminalPosition = RogueRun.TerminalPosition,
                    TerminalUsed = RogueRun.TerminalUsed,
                    StartedUtc = RogueRun.StartedUtc.ToString("O"),
                    Merc = mercenary
                };
                SingletonMonoBehaviour<FileManager>.Instance.SaveFile(FileName, SaveToJSON.CreateNode(save).ToString());
            }
            catch (Exception ex)
            {
                Debug.LogError("[RoguelikeMode] Failed to save run state: " + ex.Message);
            }
        }

        public static RogueRunSave LoadRun()
        {
            FileManager fileManager = SingletonMonoBehaviour<FileManager>.Instance;
            if (fileManager == null || !fileManager.IsFileExist(FileName))
            {
                return null;
            }
            try
            {
                string text = fileManager.LoadTextFile(FileName);
                if (string.IsNullOrEmpty(text))
                {
                    return null;
                }
                RogueRunSave save = new RogueRunSave();
                save.LoadJSON(JSON.Parse(text));
                if (save.Merc == null || string.IsNullOrEmpty(save.LocationId))
                {
                    return null;
                }
                if (save.Daily && save.DayLabel != RogueRun.TodayLabel())
                {
                    Debug.Log($"[RoguelikeMode] Suspended daily run from {save.DayLabel} is stale, removing.");
                    Delete();
                    return null;
                }
                if (RogueRun.FloorOf(save.LocationId) > RogueConfig.FloorCount)
                {
                    Debug.Log($"[RoguelikeMode] Suspended run on {save.LocationId} exceeds the current floor count, removing.");
                    Delete();
                    return null;
                }
                return save;
            }
            catch (Exception ex)
            {
                Debug.LogError("[RoguelikeMode] Failed to load run save: " + ex.Message);
                Delete();
                return null;
            }
        }

        public static void Delete()
        {
            FileManager fileManager = SingletonMonoBehaviour<FileManager>.Instance;
            if (fileManager != null && fileManager.IsFileExist(FileName))
            {
                fileManager.RemoveFile(FileName);
            }
            ExactState.Delete();
        }
    }
}
