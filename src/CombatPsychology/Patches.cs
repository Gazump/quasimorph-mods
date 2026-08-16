using System.Reflection;
using HarmonyLib;
using MGSC;
using UnityEngine;

namespace CombatPsychology
{
    internal static class EffectFields
    {
        public static readonly FieldInfo Creature = AccessTools.Field(typeof(BaseEffect), "_creature");

        public static Creature GetCreature(BaseEffect effect)
        {
            return (Creature)Creature.GetValue(effect);
        }
    }

    /// <summary>Stress from wounds; amputation also causes Shock (stun + spike).</summary>
    [HarmonyPatch(typeof(WoundSystem), nameof(WoundSystem.AddWound))]
    internal static class WoundSystem_AddWound_Patch
    {
        private static void Postfix(Creature creature, WoundCategory woundCategory)
        {
            if (!(creature is Player))
            {
                return;
            }
            switch (woundCategory)
            {
            case WoundCategory.Minor:
                StressSystem.Change(creature, PsyConfig.StressPerMinorWound);
                break;
            case WoundCategory.Normal:
                StressSystem.Change(creature, PsyConfig.StressPerWound);
                break;
            case WoundCategory.Amputation:
                StressSystem.Change(creature, PsyConfig.StressPerAmputation);
                Shock.Trigger(creature);
                break;
            }
        }
    }

    /// <summary>Player got hit: stress per hit, Shock on massive hits, Battle Focus broken,
    /// Adrenaline Rush the first time health drops critical.</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.Injure))]
    internal static class Player_Injure_Patch
    {
        private static void Postfix(Player __instance, DamageHitInfo hitInfo)
        {
            // A killing blow (including a breakdown suicide) needs no psychological reaction.
            if (hitInfo.finalDmg <= 0 || !__instance.CreatureData.Health.Alive)
            {
                return;
            }
            EffectsController effectsController = __instance.CreatureData.EffectsController;
            effectsController.RemoveAllEffects<BattleFocusBuff>();
            int hitStress = PsyConfig.StressPerHit;
            MercPsyche psyche = TraumaSystem.GetForCreature(__instance);
            if (hitInfo.info.damage == "explosion" && psyche != null)
            {
                foreach (string scar in psyche.Scars)
                {
                    hitStress = Mathf.RoundToInt((float)hitStress * (ScarCatalog.Get(scar)?.ExplosionStressMult ?? 1f));
                }
            }
            StressSystem.Change(__instance, hitStress);
            HealthInfo health = __instance.CreatureData.Health;
            if ((float)hitInfo.finalDmg >= (float)health.MaxValue * PsyConfig.BigHitHealthFraction)
            {
                Shock.Trigger(__instance);
            }
            if (health.Alive && (float)health.Value <= (float)health.MaxValue * PsyConfig.AdrenalineHealthFraction)
            {
                RaidState.NearDeath = true;
            }
            if (!RaidState.AdrenalineUsed && health.Alive && (float)health.Value <= (float)health.MaxValue * PsyConfig.AdrenalineHealthFraction)
            {
                RaidState.AdrenalineUsed = true;
                effectsController.Add(new AdrenalineRushBuff(PsyConfig.AdrenalineDurationTurns), merge: false);
                __instance.ReApplyAP(PsyConfig.AdrenalineActionPoints);
            }
        }
    }

    /// <summary>Player dealt damage: Battle Focus stacks up.</summary>
    [HarmonyPatch(typeof(Monster), nameof(Monster.Injure))]
    internal static class Monster_Injure_Patch
    {
        private static void Postfix(Monster __instance, DamageHitInfo hitInfo)
        {
            if (hitInfo.finalDmg <= 0 || !(hitInfo.damageDealer is Player player) || __instance.IsAlly(player))
            {
                return;
            }
            EffectsController effectsController = player.CreatureData.EffectsController;
            BattleFocusBuff battleFocusBuff = effectsController.First<BattleFocusBuff>();
            if (battleFocusBuff == null)
            {
                effectsController.Add(new BattleFocusBuff(1), merge: false);
            }
            else
            {
                battleFocusBuff.SetStacks(Mathf.Min(PsyConfig.BattleFocusMaxStacks, battleFocusBuff.Stacks + 1));
                effectsController.SetEffectDirty(battleFocusBuff);
            }
        }
    }

    /// <summary>Kill tracking for Bloodlust.</summary>
    [HarmonyPatch(typeof(AchievementProgress), nameof(AchievementProgress.ProcessCreatureKilledByDamage))]
    internal static class Kill_Patch
    {
        private static void Postfix(Creature victim, Creature damageDealer)
        {
            if (!(damageDealer is Player player) || victim is Player)
            {
                return;
            }
            RaidState.KillsThisTurn++;
            EffectsController effectsController = player.CreatureData.EffectsController;
            if (RaidState.KillsThisTurn + RaidState.KillsLastTurn >= PsyConfig.BloodlustKillsNeeded && !effectsController.HasAnyEffect<BloodlustBuff>())
            {
                effectsController.Add(new BloodlustBuff(PsyConfig.BloodlustDurationTurns), merge: false);
            }
        }
    }

    /// <summary>Pain overflow: Second Wind may absorb the first stun of the raid; otherwise
    /// the overwhelm adds stress on top of the vanilla stun.</summary>
    [HarmonyPatch(typeof(PainThreshold), "PainReaction")]
    internal static class PainReaction_Patch
    {
        private static bool Prefix(PainThreshold __instance)
        {
            Creature creature = EffectFields.GetCreature(__instance);
            if (!(creature is Player) || __instance.CurrentLevel < __instance.MaxLevel)
            {
                return true;
            }
            if (!RaidState.SecondWindUsed && StressSystem.GetLevel(creature) < PsyConfig.SecondWindMaxStress)
            {
                RaidState.SecondWindUsed = true;
                __instance.ChangeCurrentLevel(-__instance.MaxLevel / 2);
                creature.CreatureData.EffectsController.Add(new SecondWindBuff(2), merge: false);
                Debug.Log("[CombatPsychology] Second wind: pain stun ignored.");
                return false;
            }
            StressSystem.Change(creature, PsyConfig.StressPerPainOverflow);
            return true;
        }
    }

    /// <summary>The player's once-per-turn psychological tick, anchored to the always-present
    /// PainThreshold effect: kill-window shift and the Terror breakdown roll.</summary>
    [HarmonyPatch(typeof(PainThreshold), nameof(PainThreshold.ProcessActionPoint))]
    internal static class TurnTick_Patch
    {
        private static void Postfix(PainThreshold __instance)
        {
            Creature creature = EffectFields.GetCreature(__instance);
            if (!(creature is Player player) || !__instance.IsTurnStart)
            {
                return;
            }
            RaidState.OnPlayerTurnStarted();
            int level = StressSystem.GetLevel(player);
            RaidState.PeakStress = Mathf.Max(RaidState.PeakStress, level);
            // Night Terrors: stress never settles below the scar's floor.
            MercPsyche psyche = TraumaSystem.GetForCreature(player);
            if (psyche != null)
            {
                int floor = 0;
                foreach (string scar in psyche.Scars)
                {
                    floor = Mathf.Max(floor, ScarCatalog.Get(scar)?.StressFloor ?? 0);
                }
                if (floor > 0 && level < floor)
                {
                    StressSystem.Change(player, floor - level);
                }
            }
            Breakdown.Roll(player);
        }
    }

    /// <summary>Rising qmorphosis unsettles the merc.</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.OnQmorphStageChanged))]
    internal static class QmorphStage_Patch
    {
        private static void Postfix(Player __instance, bool stageDirty)
        {
            if (stageDirty)
            {
                StressSystem.Change(__instance, PsyConfig.StressPerQmorphStage);
            }
        }
    }

    /// <summary>Stress stage transitions: high-fortitude mercs may answer Fear with
    /// Grim Determination; dropping back below Fear removes it.</summary>
    [HarmonyPatch(typeof(StatusEffect), "HandleStageChange")]
    internal static class StageChange_Patch
    {
        private static void Postfix(StatusEffect __instance)
        {
            if (__instance.StatusEffectId != PsyConfig.StressId)
            {
                return;
            }
            Creature creature = EffectFields.GetCreature(__instance);
            if (!(creature is Player player))
            {
                return;
            }
            EffectsController effectsController = player.CreatureData.EffectsController;
            if (__instance.Stage >= 3)
            {
                if (!effectsController.HasAnyEffect<GrimDeterminationBuff>() && Random.Range(0f, 1f) <= (float)StressSystem.GetFortitude(player) * PsyConfig.GrimDeterminationChancePerFortitude)
                {
                    effectsController.Add(new GrimDeterminationBuff(endless: true), merge: false);
                }
            }
            else
            {
                effectsController.RemoveAllEffects<GrimDeterminationBuff>();
            }
        }
    }

    /// <summary>Shock: a stun plus a stress spike, at most once per turn.</summary>
    internal static class Shock
    {
        public static void Trigger(Creature creature)
        {
            if (!RaidState.ShockThisTurn)
            {
                RaidState.ShockThisTurn = true;
                // log: true routes through the vanilla combat-log stun entry.
                creature.CreatureData.EffectsController.Add(new StunEffect(PsyConfig.BreakdownStunTurns, log: true), merge: false);
                StressSystem.Change(creature, PsyConfig.StressBigHitShock);
            }
        }
    }

    /// <summary>The Terror-stage breakdown table, rolled once per player turn.</summary>
    internal static class Breakdown
    {
        public static void Roll(Player player)
        {
            int level = StressSystem.GetLevel(player);
            if (level < 75 || !player.CreatureData.Health.Alive)
            {
                return;
            }
            float num = Mathf.Lerp(PsyConfig.BreakdownBaseChance, PsyConfig.BreakdownMaxChance, (float)(level - 75) / 25f);
            float suicideMult = 1f;
            MercPsyche psyche = TraumaSystem.GetForCreature(player);
            if (psyche != null)
            {
                foreach (string scar in psyche.Scars)
                {
                    ScarDef scarDef = ScarCatalog.Get(scar);
                    if (scarDef != null)
                    {
                        num += scarDef.BreakdownChanceBonus;
                        suicideMult *= scarDef.SuicideChanceMult;
                    }
                }
            }
            if (Random.Range(0f, 1f) > num)
            {
                return;
            }
            if (level >= 100)
            {
                float num2 = (PsyConfig.SuicideChanceAt100 - (float)StressSystem.GetFortitude(player) * PsyConfig.FortitudeSuicideStep) * suicideMult;
                if (Random.Range(0f, 1f) <= num2)
                {
                    Debug.Log("[CombatPsychology] Breakdown: the merc turns their weapon on themself.");
                    KillByBreakdown(player);
                    return;
                }
            }
            // Non-lethal breakdown: the merc freezes, then vents some of the pressure.
            RaidState.BreakdownsThisRaid++;
            player.CreatureData.EffectsController.Add(new StunEffect(PsyConfig.BreakdownStunTurns, log: true), merge: false);
            StressSystem.Change(player, -PsyConfig.BreakdownStressRelease);
        }

        /// <summary>Lethal self-damage through the normal damage pipeline, so death, cloning
        /// and difficulty revive rules all apply exactly as for any other death.</summary>
        private static void KillByBreakdown(Player player)
        {
            int num = player.CreatureData.Health.MaxValue * 100;
            DamageHitInfo damageHitInfo = new DamageHitInfo(1, 1f, new DmgInfo
            {
                critChance = 0f,
                minDmg = num,
                maxDmg = num,
                damage = "pierce"
            }, 1f);
            damageHitInfo.damageDealer = player;
            damageHitInfo.dangerPosition = player.CreatureData.Position;
            player.Injure(damageHitInfo);
        }
    }

    /// <summary>Fortitude readout appended to the stress tooltip.</summary>
    [HarmonyPatch(typeof(TooltipFactory), nameof(TooltipFactory.BuildStatusEffectTooltip))]
    internal static class StressTooltip_Patch
    {
        private static void Postfix(TooltipFactory __instance, StatusEffect statusEffect)
        {
            if (statusEffect.StatusEffectId != PsyConfig.StressId)
            {
                return;
            }
            Creature creature = EffectFields.GetCreature(statusEffect);
            if (creature != null)
            {
                int fortitude = StressSystem.GetFortitude(creature);
                int num = Mathf.RoundToInt(StressSystem.GetGainMultiplier(creature) * 100f);
                __instance.AddPanelToTooltip().SetMultilineName(Localization.Get("ui.psy.fortitude") + ": " + fortitude + " (" + Localization.Get("ui.psy.stressgain") + " " + num + "%)").SetNameColor(Colors.AltGreen);
            }
        }
    }
}
