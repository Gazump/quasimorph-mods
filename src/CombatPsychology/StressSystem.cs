using MGSC;
using UnityEngine;

namespace CombatPsychology
{
    /// <summary>Core stress bookkeeping: read, gain (fortitude- and difficulty-scaled), relieve.</summary>
    public static class StressSystem
    {
        /// <summary>Set once per raid from the DungeonStarted hook; null outside raids.</summary>
        public static Difficulty Difficulty;

        public static StatusEffect Find(Creature creature)
        {
            if (creature == null)
            {
                return null;
            }
            return StatusEffectsSystem.FindById(creature.CreatureData.EffectsController, PsyConfig.StressId);
        }

        public static int GetLevel(Creature creature)
        {
            StatusEffect statusEffect = Find(creature);
            return statusEffect?.Level ?? 0;
        }

        public static int GetStage(Creature creature)
        {
            StatusEffect statusEffect = Find(creature);
            return statusEffect?.Stage ?? 0;
        }

        /// <summary>Ship-side fortitude (no raid bonuses): base + perks + scars.</summary>
        public static int GetFortitudeForMerc(Mercenary mercenary)
        {
            if (mercenary == null)
            {
                return PsyConfig.FortitudeBase;
            }
            int num = PsyConfig.FortitudeBase + PerkSystem.GetPerkParameterSumInt(mercenary.CreatureData, "IFortitude");
            return num + TraumaSystem.GetScarFortitudeMod(PsycheStore.Current.Find(mercenary.ProfileId));
        }

        public static int GetFortitude(Creature creature)
        {
            if (!(creature is Player player))
            {
                return PsyConfig.FortitudeBase;
            }
            int num = GetFortitudeForMerc(player.Mercenary);
            if (RaidState.SurvivorsHighActive)
            {
                num += 2;
            }
            return num;
        }

        public static float GetGainMultiplier(Creature creature)
        {
            float num = 1f - (float)(GetFortitude(creature) - PsyConfig.FortitudeBase) * PsyConfig.FortitudeGainStep;
            if (Difficulty?.Preset != null && Difficulty.Preset.QmorphLevelGrowth > 0f)
            {
                num *= Difficulty.Preset.QmorphLevelGrowth;
            }
            num *= TraumaSystem.GetScarGainMult(TraumaSystem.GetForCreature(creature));
            return Mathf.Clamp(num, PsyConfig.MinGainMult, PsyConfig.MaxGainMult);
        }

        /// <summary>Add (scaled) stress to the player. Negative amounts relieve and are not scaled.</summary>
        public static void Change(Creature creature, int amount)
        {
            if (creature == null || !(creature is Player) || amount == 0)
            {
                return;
            }
            if (amount > 0)
            {
                amount = Mathf.Max(1, Mathf.RoundToInt((float)amount * GetGainMultiplier(creature)));
            }
            EffectsController effectsController = creature.CreatureData.EffectsController;
            StatusEffect statusEffect = Find(creature);
            if (statusEffect == null)
            {
                if (amount <= 0)
                {
                    return;
                }
                statusEffect = new StatusEffect(PsyConfig.StressId, Mathf.Clamp(amount, 1, 100));
                effectsController.Add(statusEffect, merge: false);
            }
            else
            {
                statusEffect.ApplyLevelChange(amount, force: false, updateStage: true);
                if (statusEffect.Level <= 0)
                {
                    effectsController.Remove(statusEffect.ID);
                }
                else
                {
                    effectsController.SetEffectDirty(statusEffect);
                }
            }
            RaidState.PeakStress = Mathf.Max(RaidState.PeakStress, statusEffect.Level);
        }
    }
}
