namespace CombatPsychology
{
    public static class PsyConfig
    {
        public const string StressId = "stress";
        public const string SedativeAddictionId = "sedativeAddiction";
        public const string SedativeItemId = "qm_psy_sedative";

        public const int StressPerHit = 2;
        public const int StressPerMinorWound = 4;
        public const int StressPerWound = 10;
        public const int StressPerAmputation = 25;
        public const int StressPerPainOverflow = 8;
        public const int StressPerQmorphStage = 8;
        public const int StressBigHitShock = 12;
        public const int StressBloodlustComedown = 15;

        public const float BigHitHealthFraction = 0.30f;

        public const float BreakdownBaseChance = 0.15f;
        public const float BreakdownMaxChance = 0.50f;
        public const float SuicideChanceAt100 = 0.25f;
        public const int BreakdownStunTurns = 1;
        public const int BreakdownStressRelease = 15;

        public const int FortitudeBase = 3;
        public const float FortitudeGainStep = 0.07f;
        public const float MinGainMult = 0.5f;
        public const float MaxGainMult = 1.5f;
        public const float FortitudeSuicideStep = 0.04f;

        public const int BloodlustKillsNeeded = 3;
        public const int BloodlustDurationTurns = 5;
        public const float BloodlustMeleeAccuracy = 0.15f;
        public const float BloodlustPainRegen = 10f;

        public const int BattleFocusMaxStacks = 3;
        public const float BattleFocusAccuracyPerStack = 0.04f;

        public const float AdrenalineHealthFraction = 0.30f;
        public const int AdrenalineDurationTurns = 3;
        public const float AdrenalinePainRegen = 15f;
        public const int AdrenalineActionPoints = 2;

        public const int SecondWindMaxStress = 50;

        public const float GrimDeterminationChancePerFortitude = 0.10f;
        public const float GrimDeterminationAccuracy = 0.20f;

        public const int SedativeStressRelief = -35;
        public const float SedativeAddictionChance = 0.12f;
        public const float SedativeDropWeightFactor = 0.5f;
        public const int AlcoholStressRelief = -12;
        public const int NicotineStressRelief = -8;
    }
}
