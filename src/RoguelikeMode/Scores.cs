using System;
using System.Collections.Generic;
using MGSC;
using SimpleJSON;
using UnityEngine;

namespace RoguelikeMode
{
    public class RogueScoreEntry : IWrapTypeOnSave
    {
        [Save]
        public string Day { get; set; }

        [Save]
        public bool Daily { get; set; }

        [Save]
        public int Tier { get; set; }

        [Save]
        public string ProfileId { get; set; }

        [Save]
        public string ClassId { get; set; }

        [Save]
        public int Floor { get; set; }

        [Save]
        public int Kills { get; set; }

        [Save]
        public int Turns { get; set; }

        [Save]
        public int DamageTaken { get; set; }

        [Save]
        public int Score { get; set; }

        [Save]
        public bool Victory { get; set; }

        [Save]
        public bool Modded { get; set; }

        [Save]
        public string PlayedAtUtc { get; set; }
    }

    public class RogueScoreStore
    {
        [Save]
        public List<RogueScoreEntry> Entries { get; set; } = new List<RogueScoreEntry>();
    }

    public static class ScoreSystem
    {
        private const string FileName = "roguelike_scores.dat";
        private const int MaxEntries = 50;

        public static int Compute(int floor, int kills, bool victory, int damageTaken, RogueTier tier)
        {
            float score = floor * 1000 + kills * 20 + (victory ? 5000 : 0) - Mathf.Min(damageTaken, 1000);
            if (tier == RogueTier.Easy)
            {
                score *= 0.75f;
            }
            else if (tier == RogueTier.Hard)
            {
                score *= 1.25f;
            }
            return Mathf.Max(0, Mathf.RoundToInt(score));
        }

        public static RogueScoreEntry Record(string profileId, string classId, int turns, bool victory)
        {
            RogueScoreEntry entry = new RogueScoreEntry
            {
                Day = RogueRun.DayLabel,
                Daily = RogueRun.Daily,
                Tier = (int)RogueRun.Tier,
                ProfileId = profileId,
                ClassId = classId,
                Floor = RogueRun.DeepestFloor,
                Kills = RogueRun.PlayerKills,
                Turns = turns,
                DamageTaken = RogueRun.DamageTaken,
                Score = Compute(RogueRun.DeepestFloor, RogueRun.PlayerKills, victory, RogueRun.DamageTaken, RogueRun.Tier),
                Victory = victory,
                Modded = RogueRun.ActiveMods.Count > 0,
                PlayedAtUtc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm")
            };
            RogueScoreStore store = Load();
            store.Entries.Add(entry);
            store.Entries.Sort((a, b) => b.Score.CompareTo(a.Score));
            if (store.Entries.Count > MaxEntries)
            {
                store.Entries.RemoveRange(MaxEntries, store.Entries.Count - MaxEntries);
            }
            Save(store);
            return entry;
        }

        public static int BestScore()
        {
            RogueScoreStore store = Load();
            return (store.Entries.Count > 0) ? store.Entries[0].Score : 0;
        }

        public static RogueScoreStore Load()
        {
            RogueScoreStore store = new RogueScoreStore();
            FileManager fileManager = SingletonMonoBehaviour<FileManager>.Instance;
            if (fileManager == null || !fileManager.IsFileExist(FileName))
            {
                return store;
            }
            try
            {
                string text = fileManager.LoadTextFile(FileName);
                if (!string.IsNullOrEmpty(text))
                {
                    store.LoadJSON(JSON.Parse(text));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[RoguelikeMode] Failed to load scores: " + ex.Message);
                store = new RogueScoreStore();
            }
            return store;
        }

        private static void Save(RogueScoreStore store)
        {
            try
            {
                SingletonMonoBehaviour<FileManager>.Instance.SaveFile(FileName, SaveToJSON.CreateNode(store).ToString());
            }
            catch (Exception ex)
            {
                Debug.LogError("[RoguelikeMode] Failed to save scores: " + ex.Message);
            }
        }
    }
}
