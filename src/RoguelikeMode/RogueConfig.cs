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
        public const int FloorCount = 5;
        public const int MissionDifficultyRating = 100;
        public const string StoryId = "RogueDescent";
        public const string EasyCaptionKey = "ui.rogue.easy";
        public const string NormalCaptionKey = "ui.rogue.normal";
        public const string HardCaptionKey = "ui.rogue.hard";
        public const string BackCaptionKey = "ui.rogue.back";
        public const string ResumeCaptionKey = "ui.rogue.resume";
        public const string DiveButtonCaptionKey = "ui.dive.menubutton";
        public const string TerminalCaptionKey = "ui.dive.terminal";
        public const string TradeInCaptionKey = "ui.dive.tradein";
        public const float TradeInRate = 0.85f;
        public static readonly string[] BlockedContainerIds = { "weapon_container", "armor_container", "security_clothing_container" };
        public static readonly string[] TerminalContainerIds = { "data_container", "matrix_box", "common_locker" };
        public const int LearnClassContextBind = 5100;
        public const string HarmonyId = "quasimorph.roguelikemode";
        public const float TopMonsterPointsMult = 1.35f;
        public const float TopItemPointsMult = 1.25f;
        public const float FirstFloorMapScale = 0.65f;
        public const int MinRoomsFirstFloor = 6;
        public const int MaxRoomsLastFloor = 12;

        public static int TechLevelForFloor(int floor)
        {
            return Mathf.Clamp(floor, 1, Data.Global.MaxTechLevel);
        }
    }
}
