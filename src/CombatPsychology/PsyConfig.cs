namespace CombatPsychology
{
    /// <summary>All tuning knobs for v1 in one place.</summary>
    public static class PsyConfig
    {
        public const string StressId = "stress";
        public const string SedativeAddictionId = "sedativeAddiction";
        public const string SedativeItemId = "qm_psy_sedative";

        // --- Stress gains (before fortitude/difficulty multipliers) ---
        public const int StressPerHit = 2;
        public const int StressPerMinorWound = 4;
        public const int StressPerWound = 10;
        public const int StressPerAmputation = 25;
        public const int StressPerPainOverflow = 8;
        public const int StressPerQmorphStage = 8;
        public const int StressBigHitShock = 12;
        public const int StressBloodlustComedown = 15;

        // A single hit for at least this fraction of max health triggers Shock.
        public const float BigHitHealthFraction = 0.30f;

        // --- Breakdown (stress stage 4, rolled once per turn) ---
        public const float BreakdownBaseChance = 0.15f;   // at 75 stress
        public const float BreakdownMaxChance = 0.50f;    // at 100 stress
        public const float SuicideChanceAt100 = 0.25f;    // breakdown at 100 stress may turn lethal
        public const int BreakdownStunTurns = 1;
        public const int BreakdownStressRelease = 15;     // a non-lethal breakdown vents some stress

        // --- Fortitude ---
        // Every merc starts here; perks with the "IFortitude" int parameter add to it.
        public const int FortitudeBase = 3;
        // Each point above/below base shifts stress gain by this much.
        public const float FortitudeGainStep = 0.07f;
        public const float MinGainMult = 0.5f;
        public const float MaxGainMult = 1.5f;
        // Each fortitude point shaves this off the suicide roll.
        public const float FortitudeSuicideStep = 0.04f;

        // --- Positive effects ---
        public const int BloodlustKillsNeeded = 3;        // kills within the current+previous turn
        public const int BloodlustDurationTurns = 5;
        public const float BloodlustMeleeAccuracy = 0.15f;
        public const float BloodlustPainRegen = 10f;

        public const int BattleFocusMaxStacks = 3;
        public const float BattleFocusAccuracyPerStack = 0.04f;

        public const float AdrenalineHealthFraction = 0.30f;
        public const int AdrenalineDurationTurns = 3;
        public const float AdrenalinePainRegen = 15f;
        public const int AdrenalineActionPoints = 2;

        public const int SecondWindMaxStress = 50;        // only triggers while relatively calm

        public const float GrimDeterminationChancePerFortitude = 0.10f;
        public const float GrimDeterminationAccuracy = 0.20f;

        // --- Treatments ---
        public const int SedativeStressRelief = -35;
        public const float SedativeAddictionChance = 0.12f;
        public const int AlcoholStressRelief = -12;
        public const int NicotineStressRelief = -8;
    }
}
