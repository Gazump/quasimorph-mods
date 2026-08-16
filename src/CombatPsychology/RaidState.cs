namespace CombatPsychology
{
    public static class RaidState
    {
        public static bool SecondWindUsed;
        public static bool AdrenalineUsed;
        public static int KillsThisTurn;
        public static int KillsLastTurn;
        public static bool ShockThisTurn;

        public static int PeakStress;
        public static int BreakdownsThisRaid;
        public static bool NearDeath;

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
