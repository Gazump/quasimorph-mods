using System.Collections.Generic;

namespace CombatPsychology
{
    public class ScarDef
    {
        public string Id;
        public bool Positive;
        /// <summary>Added to the merc's Fortitude while carried.</summary>
        public int FortitudeMod;
        /// <summary>Multiplies all stress gain while carried (1 = neutral).</summary>
        public float StressGainMult = 1f;
        /// <summary>Stress applied at the start of every raid.</summary>
        public int StartingStress;
        /// <summary>Status effect (id, level) applied at the start of every raid.</summary>
        public string StartingStatusId;
        public int StartingStatusLevel;
        /// <summary>Wound-effects carried all raid via the ScarsEffect buff (id → value).</summary>
        public Dictionary<string, float> WoundEffects;
        /// <summary>Stress from explosion damage is multiplied by this (Shell Shock).</summary>
        public float ExplosionStressMult = 1f;
        /// <summary>In-raid stress can never fall below this (Night Terrors).</summary>
        public int StressFloor;
        /// <summary>Added to the per-turn breakdown chance at Terror.</summary>
        public float BreakdownChanceBonus;
        /// <summary>Multiplies the lethal-breakdown roll (Death Wish).</summary>
        public float SuicideChanceMult = 1f;
        /// <summary>True if minted by the normal trauma-threshold roll (Death Wish and the
        /// positive scars have their own conditions instead).</summary>
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
