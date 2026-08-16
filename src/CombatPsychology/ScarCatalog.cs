using System.Collections.Generic;

namespace CombatPsychology
{
    public class ScarDef
    {
        public string Id;
        public bool Positive;
        public int FortitudeMod;
        public float StressGainMult = 1f;
        public int StartingStress;
        public string StartingStatusId;
        public int StartingStatusLevel;
        public Dictionary<string, float> WoundEffects;
        public float ExplosionStressMult = 1f;
        public int StressFloor;
        public float BreakdownChanceBonus;
        public float SuicideChanceMult = 1f;
        public bool InThresholdPool = true;
    }

    public static class ScarCatalog
    {
        public const string ShellShock = "shell_shock";
        public const string NightTerrors = "night_terrors";
        public const string Depression = "depression";
        public const string SubstanceDependence = "substance_dependence";
        public const string DeathWish = "death_wish";
        public const string ColdBlood = "cold_blood";

        public static readonly List<ScarDef> All = new List<ScarDef>
        {
            new ScarDef
            {
                Id = ShellShock,
                StartingStress = 20,
                ExplosionStressMult = 2f
            },
            new ScarDef
            {
                Id = NightTerrors,
                FortitudeMod = -1,
                StressFloor = 10
            },
            new ScarDef
            {
                Id = Depression,
                FortitudeMod = -2,
                StressGainMult = 1.25f,
                WoundEffects = new Dictionary<string, float> { { "perk_exp_modifier", -0.25f } }
            },
            new ScarDef
            {
                Id = SubstanceDependence,
                StartingStatusId = PsyConfig.SedativeAddictionId,
                StartingStatusLevel = 25
            },
            new ScarDef
            {
                Id = DeathWish,
                InThresholdPool = false,
                BreakdownChanceBonus = 0.1f,
                SuicideChanceMult = 2f,
                WoundEffects = new Dictionary<string, float> { { "more_dmg_mult", 0.1f } }
            },
            new ScarDef
            {
                Id = ColdBlood,
                Positive = true,
                InThresholdPool = false,
                FortitudeMod = 1,
                StressGainMult = 0.8f
            }
        };

        public static ScarDef Get(string id)
        {
            foreach (ScarDef scarDef in All)
            {
                if (scarDef.Id == id)
                {
                    return scarDef;
                }
            }
            return null;
        }
    }
}
