using System.Collections.Generic;
using MGSC;
using UnityEngine;

namespace CombatPsychology
{
    public class RaidDebrief
    {
        public string ProfileId;
        public int TraumaBefore;
        public int TraumaAfter;
        public List<string> NewScars = new List<string>();
        public bool Died;

        public int Delta => TraumaAfter - TraumaBefore;
    }

    public static class TraumaSystem
    {
        public static RaidDebrief LastDebrief;

        public const int TraumaMax = 100;
        public const int DeathTrauma = 15;
        public const int RestfulRecovery = 6;
        public const int HighStressTrauma = 12;
        public const int MidStressTrauma = 6;
        public const int TraumaPerAmputation = 8;
        public const int TraumaPerBreakdown = 5;
        public const int NearDeathTrauma = 4;
        public const int ColdBloodStreakNeeded = 3;
        public const int DeathWishMinTrauma = 80;
        private static readonly int[] Thresholds = { 25, 50, 75 };

        public static MercPsyche GetForCreature(Creature creature)
        {
            if (creature is Player player && player.Mercenary != null)
            {
                return PsycheStore.Current.Find(player.Mercenary.ProfileId);
            }
            return null;
        }

        public static int GetScarFortitudeMod(MercPsyche psyche)
        {
            if (psyche == null)
            {
                return 0;
            }
            int num = 0;
            foreach (string scar in psyche.Scars)
            {
                num += ScarCatalog.Get(scar)?.FortitudeMod ?? 0;
            }
            return num;
        }

        public static float GetScarGainMult(MercPsyche psyche)
        {
            if (psyche == null)
            {
                return 1f;
            }
            float num = 1f;
            foreach (string scar in psyche.Scars)
            {
                num *= ScarCatalog.Get(scar)?.StressGainMult ?? 1f;
            }
            return num;
        }

        public static void ChangeTrauma(MercPsyche psyche, int delta)
        {
            int trauma = psyche.Trauma;
            psyche.Trauma = Mathf.Clamp(trauma + delta, 0, TraumaMax);
            foreach (int threshold in Thresholds)
            {
                if (trauma < threshold && psyche.Trauma >= threshold)
                {
                    MintScar(psyche);
                }
            }
            if (psyche.Trauma >= DeathWishMinTrauma && psyche.HasScar(ScarCatalog.Depression) && !psyche.HasScar(ScarCatalog.DeathWish))
            {
                psyche.Scars.Add(ScarCatalog.DeathWish);
                Debug.Log($"[CombatPsychology] {psyche.ProfileId} developed a death wish.");
            }
        }

        private static void MintScar(MercPsyche psyche)
        {
            List<ScarDef> list = new List<ScarDef>();
            foreach (ScarDef scarDef in ScarCatalog.All)
            {
                if (scarDef.InThresholdPool && !psyche.HasScar(scarDef.Id))
                {
                    list.Add(scarDef);
                }
            }
            if (list.Count > 0)
            {
                string id = list[Random.Range(0, list.Count)].Id;
                psyche.Scars.Add(id);
                Debug.Log($"[CombatPsychology] {psyche.ProfileId} gained scar: {id}");
            }
        }

        public static void ProcessRaidReturn(Mercenary mercenary)
        {
            MercPsyche psyche = PsycheStore.Current.GetOrCreate(mercenary.ProfileId);
            RaidDebrief debrief = BeginDebrief(psyche);
            StatusEffect statusEffect = StatusEffectsSystem.FindById(mercenary.CreatureData.EffectsController, PsyConfig.StressId);
            int exitStress = statusEffect?.Level ?? 0;
            int peak = Mathf.Max(RaidState.PeakStress, exitStress);
            int amputations = 0;
            foreach (BaseEffect item in mercenary.CreatureData.EffectsController)
            {
                if (item is BodyPartWound { IsAmputation: not false })
                {
                    amputations++;
                }
            }
            int num = ((peak >= 75) ? HighStressTrauma : ((peak >= 50) ? MidStressTrauma : 0));
            num += amputations * TraumaPerAmputation;
            num += RaidState.BreakdownsThisRaid * TraumaPerBreakdown;
            if (RaidState.NearDeath)
            {
                num += NearDeathTrauma;
            }
            if (num > 0)
            {
                ChangeTrauma(psyche, num);
            }
            else if (peak < 25)
            {
                ChangeTrauma(psyche, -RestfulRecovery);
            }
            psyche.SurvivorsHighPending = exitStress >= 75;
            if (peak >= 50 && RaidState.BreakdownsThisRaid == 0)
            {
                psyche.CleanRaidStreak++;
                if (psyche.CleanRaidStreak >= ColdBloodStreakNeeded && !psyche.HasScar(ScarCatalog.ColdBlood))
                {
                    psyche.Scars.Add(ScarCatalog.ColdBlood);
                    Debug.Log($"[CombatPsychology] {psyche.ProfileId} earned Cold Blood.");
                }
            }
            else if (RaidState.BreakdownsThisRaid > 0)
            {
                psyche.CleanRaidStreak = 0;
            }
            FinishDebrief(debrief, psyche, died: false);
            Debug.Log($"[CombatPsychology] Raid debrief {psyche.ProfileId}: peak stress {peak}, amputations {amputations}, breakdowns {RaidState.BreakdownsThisRaid} -> trauma {psyche.Trauma}");
        }

        public static void ProcessDeath(Mercenary mercenary)
        {
            MercPsyche psyche = PsycheStore.Current.GetOrCreate(mercenary.ProfileId);
            RaidDebrief debrief = BeginDebrief(psyche);
            psyche.CleanRaidStreak = 0;
            psyche.SurvivorsHighPending = false;
            ChangeTrauma(psyche, DeathTrauma);
            FinishDebrief(debrief, psyche, died: true);
            Debug.Log($"[CombatPsychology] {psyche.ProfileId} died; clone carries the memory -> trauma {psyche.Trauma}");
        }

        private static RaidDebrief BeginDebrief(MercPsyche psyche)
        {
            return new RaidDebrief
            {
                ProfileId = psyche.ProfileId,
                TraumaBefore = psyche.Trauma,
                NewScars = new List<string>(psyche.Scars)
            };
        }

        private static void FinishDebrief(RaidDebrief debrief, MercPsyche psyche, bool died)
        {
            List<string> before = debrief.NewScars;
            debrief.NewScars = new List<string>();
            foreach (string scar in psyche.Scars)
            {
                if (!before.Contains(scar))
                {
                    debrief.NewScars.Add(scar);
                }
            }
            debrief.TraumaAfter = psyche.Trauma;
            debrief.Died = died;
            LastDebrief = debrief;
        }

        public static void ApplyAtRaidStart(Mercenary mercenary)
        {
            MercPsyche psyche = PsycheStore.Current.Find(mercenary.ProfileId);
            if (psyche == null)
            {
                return;
            }
            if (psyche.SurvivorsHighPending)
            {
                psyche.SurvivorsHighPending = false;
                RaidState.SurvivorsHighActive = true;
                mercenary.CreatureData.EffectsController.Add(new SurvivorsHighBuff(), merge: false);
            }
            if (psyche.Scars.Count == 0)
            {
                return;
            }
            mercenary.CreatureData.EffectsController.Add(new ScarsEffect(), merge: false);
            int num = 0;
            foreach (string scar in psyche.Scars)
            {
                ScarDef scarDef = ScarCatalog.Get(scar);
                if (scarDef == null)
                {
                    continue;
                }
                num = Mathf.Max(num, scarDef.StartingStress);
                if (!string.IsNullOrEmpty(scarDef.StartingStatusId))
                {
                    StatusEffectsSystem.ApplyStatusEffect(scarDef.StartingStatusId, mercenary.CreatureData.EffectsController, scarDef.StartingStatusLevel);
                }
            }
            if (num > 0)
            {
                StatusEffectsSystem.ApplyStatusEffect(PsyConfig.StressId, mercenary.CreatureData.EffectsController, num);
            }
        }
    }
}
