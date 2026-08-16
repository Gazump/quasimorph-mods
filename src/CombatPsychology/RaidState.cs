namespace CombatPsychology
{
    /// <summary>
    /// Per-raid runtime flags. Deliberately not saved: reloading a mid-raid save re-arms
    /// once-per-raid effects, which is acceptable for v1. Trauma-relevant counters are
    /// read by TraumaSystem at raid exit (RaidState is only reset when the NEXT raid starts,
    /// so they are still valid during post-mission processing).
    /// </summary>
    public static class RaidState
    {
        public static bool SecondWindUsed;
        public static bool AdrenalineUsed;
        public static int KillsThisTurn;
        public static int KillsLastTurn;
        public static bool ShockThisTurn;

        // Trauma inputs
        public static int PeakStress;
        public static int BreakdownsThisRaid;
        public static bool NearDeath;

        /// <summary>Survivor's High consumed for this raid (+2 Fortitude).</summary>
        public static bool SurvivorsHighActive;

        public static void Reset()
        {
            SecondWindUsed = false;
            AdrenalineUsed = false;
            KillsThisTurn = 0;
            KillsLastTurn = 0;
            ShockThisTurn = false;
            PeakStress = 0;
            BreakdownsThisRaid = 0;
            NearDeath = false;
            SurvivorsHighActive = false;
        }

        public static void OnPlayerTurnStarted()
        {
            KillsLastTurn = KillsThisTurn;
            KillsThisTurn = 0;
            ShockThisTurn = false;
        }
    }
}
