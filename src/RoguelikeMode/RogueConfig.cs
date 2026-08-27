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
        public const int DefaultFloorCount = 5;
        public const int EscalationSpanCap = 10;
        public static readonly int[] FloorChoices = { 3, 5, 10, 99 };
        public static readonly string[] FloorChoiceLabels = { "QUICK", "NORMAL", "LONG", "SILLY" };
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
        public const int InstallImplantContextBind = 5102;
        public const int InstallProstheticContextBind = 5103;
        public const string HarmonyId = "quasimorph.roguelikemode";
        public const float TopMonsterPointsMult = 1.35f;
        public const float TopItemPointsMult = 1.25f;
        public const float FirstFloorMapScale = 0.65f;
        public const int MinRoomsFirstFloor = 6;
        public const int MaxRoomsLastFloor = 12;

        public static float FloorProgress(int floor)
        {
            int span = Mathf.Min(RogueRun.TotalFloors, EscalationSpanCap);
            if (span <= 1)
            {
                return 1f;
            }
            return Mathf.Clamp01((floor - 1) / (float)(span - 1));
        }

        public static int TechLevelForFloor(int floor)
        {
            int max = Data.Global.MaxTechLevel;
            return Mathf.Clamp(1 + Mathf.CeilToInt(FloorProgress(floor) * (max - 1)), 1, max);
        }
    }
}
