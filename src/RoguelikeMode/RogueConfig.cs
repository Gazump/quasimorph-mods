using MGSC;
using UnityEngine;

namespace RoguelikeMode
{
    public enum RogueTier
    {
        Easy,
        Normal,
        Hard
    }

    public static class RogueConfig
    {
        public const string KeycardId = "rogue_golden_keycard";
        public const int FloorCount = 10;
        public const int MissionDifficultyRating = 100;
        public const string StoryId = "RogueDescent";
        public const string EasyCaptionKey = "ui.rogue.easy";
        public const string NormalCaptionKey = "ui.rogue.normal";
        public const string HardCaptionKey = "ui.rogue.hard";
        public const string BackCaptionKey = "ui.rogue.back";
        public const string ResumeCaptionKey = "ui.rogue.resume";
        public const string DiveButtonCaptionKey = "ui.dive.menubutton";
        public const int LearnClassContextBind = 5100;
        public const string HarmonyId = "quasimorph.roguelikemode";
        public const float TopMonsterPointsMult = 1.35f;
        public const float TopItemPointsMult = 1.25f;
        public const float FirstFloorMapScale = 0.65f;
        public const int MinRoomsFirstFloor = 6;
        public const int MaxRoomsLastFloor = 12;

        public static int TechLevelForFloor(int floor)
        {
            return Mathf.Clamp(1 + (floor - 1) / 3, 1, Data.Global.MaxTechLevel);
        }
    }
}
