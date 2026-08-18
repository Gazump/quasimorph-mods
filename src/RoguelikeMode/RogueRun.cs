using System;
using System.Collections.Generic;

namespace RoguelikeMode
{
    public static class RogueRun
    {
        public static bool Active;
        public static bool Daily = true;
        public static RogueTier Tier = RogueTier.Normal;
        public static string DayLabel = string.Empty;
        public static int DaySeed;
        public static int CandidateIndex;
        public static int CurrentFloor;
        public static int DeepestFloor;
        public static int PlayerKills;
        public static int DamageTaken;
        public static string CurrentLocationId = string.Empty;
        public static string LastSummary = string.Empty;
        public static List<string> ActiveMods = new List<string>();

        private static int _generateAttempt;

        public static void PrepareDay(bool daily)
        {
            Daily = daily;
            if (daily)
            {
                DayLabel = DateTime.UtcNow.ToString("yyyy-MM-dd");
                DaySeed = Fnv("quasimorph-rogue-" + DayLabel);
            }
            else
            {
                DayLabel = "random";
                DaySeed = Fnv("quasimorph-rogue-random-" + DateTime.UtcNow.Ticks + "-" + Environment.TickCount);
            }
            Active = false;
        }

        public static void BeginRunState(int candidateIndex, RogueTier tier)
        {
            CandidateIndex = candidateIndex;
            Tier = tier;
            CurrentFloor = 0;
            DeepestFloor = 0;
            PlayerKills = 0;
            DamageTaken = 0;
            CurrentLocationId = string.Empty;
            _generateAttempt = 0;
            Active = false;
        }

        public static int SeedFor(string tag)
        {
            return Fnv(DaySeed.ToString("X8") + ":" + tag);
        }

        public static void ResetAttempts(string locationId)
        {
            CurrentLocationId = locationId;
            _generateAttempt = 0;
        }

        public static int NextAttemptSeed()
        {
            return SeedFor("gen:" + CurrentLocationId + ":" + _generateAttempt++);
        }

        public static int FloorOf(string locationId)
        {
            if (string.IsNullOrEmpty(locationId) || !locationId.StartsWith("stage"))
            {
                return 0;
            }
            int.TryParse(locationId.Substring(5), out int floor);
            return floor;
        }

        private static int Fnv(string text)
        {
            unchecked
            {
                uint hash = 2166136261u;
                foreach (char c in text)
                {
                    hash = (hash ^ c) * 16777619u;
                }
                return (int)hash;
            }
        }
    }
}
