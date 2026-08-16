namespace CombatPsychology
{
    /// <summary>
    /// Per-raid runtime flags. Deliberately not saved: reloading a mid-raid save re-arms
    /// once-per-raid effects, which is acceptable for v1.
    /// </summary>
    public static class RaidState
    {
        public static bool SecondWindUsed;
        public static bool AdrenalineUsed;
        public static int KillsThisTurn;
        public static int KillsLastTurn;
        public static bool ShockThisTurn;

        public static void Reset()
        {
            SecondWindUsed = false;
            AdrenalineUsed = false;
            KillsThisTurn = 0;
            KillsLastTurn = 0;
            ShockThisTurn = false;
        }

        public static void OnPlayerTurnStarted()
        {
            KillsLastTurn = KillsThisTurn;
            KillsThisTurn = 0;
            ShockThisTurn = false;
        }
    }
}
