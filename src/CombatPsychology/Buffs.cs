using System.Collections.Generic;
using MGSC;

namespace CombatPsychology
{
    /// <summary>
    /// Base for this mod's buffs. Stat changes are expressed as vanilla WoundEffect
    /// sub-effects (same pattern StatusEffect uses), so CreatureData's stat sums pick
    /// them up without any patching. Sub-effect ids are saved and cleaned up on removal.
    /// </summary>
    public abstract class PsyBuff : Buff, IEffectWithView
    {
        [Save]
        public List<int> SubEffects { get; private set; } = new List<int>();

        public virtual float ViewValue => Duration;

        public virtual EffectViewShowValueMode ShowValueMode => EffectViewShowValueMode.ShowMax;

        public virtual EffectViewShowValueFormat ShowValueFormat => EffectViewShowValueFormat.Raw;

        public bool Show => true;

        public virtual bool IsRedView => false;

        public bool BlinkOnChange => true;

        protected PsyBuff()
        {
        }

        protected PsyBuff(int duration)
        {
            Duration = duration;
            OriginalDuration = duration;
        }

        protected void AddSubEffect(string effectId, float value)
        {
            WoundEffect woundEffect = Singleton<EffectFactory>.Instance.CreateWoundEffect(effectId, BuffId, BuffId, value, default(WoundBonus));
            if (woundEffect != null)
            {
                _creature.CreatureData.EffectsController.Add(woundEffect, merge: false);
                SubEffects.Add(woundEffect.ID);
            }
        }

        /// <summary>The live wound-effects backing this buff, for tooltip stat lines.</summary>
        public List<WoundEffect> ResolveSubEffects()
        {
            List<WoundEffect> list = new List<WoundEffect>();
            if (_getEffect == null)
            {
                return list;
            }
            foreach (int subEffect in SubEffects)
            {
                WoundEffect woundEffect = _getEffect.First<WoundEffect>(subEffect);
                if (woundEffect != null)
                {
                    list.Add(woundEffect);
                }
            }
            return list;
        }

        protected void ClearSubEffects()
        {
            if (_creature == null)
            {
                SubEffects.Clear();
                return;
            }
            foreach (int subEffect in SubEffects)
            {
                _creature.CreatureData.EffectsController.Remove(subEffect);
            }
            SubEffects.Clear();
        }

        public override void ProcessActionPoint()
        {
            if (_creature != null && IsTurnStart)
            {
                base.ProcessActionPoint();
            }
        }

        public override void OnRemoved()
        {
            base.OnRemoved();
            ClearSubEffects();
        }
    }

    /// <summary>Kill streak high: melee accuracy and pain relief now, a stress bill when it fades.</summary>
    public class BloodlustBuff : PsyBuff
    {
        public BloodlustBuff()
        {
        }

        public BloodlustBuff(int duration)
            : base(duration)
        {
        }

        public override void OnAdded()
        {
            base.OnAdded();
            AddSubEffect("melee_accuracy", PsyConfig.BloodlustMeleeAccuracy);
            AddSubEffect("pain_threshold_regen", PsyConfig.BloodlustPainRegen);
        }

        public override void OnRemoved()
        {
            base.OnRemoved();
            // The comedown only bites while still in a raid and alive; EffectsController.Clear()
            // between raids never calls OnRemoved, so this cannot leak into the ship phase.
            if (_creature is Player && _creature.CreatureData.Health.Alive && StressSystem.Difficulty != null)
            {
                StressSystem.Change(_creature, PsyConfig.StressBloodlustComedown);
            }
        }
    }

    /// <summary>Stacking ranged accuracy for consecutive turns of dealing damage without taking any.</summary>
    public class BattleFocusBuff : PsyBuff
    {
        [Save]
        public int Stacks { get; set; }

        public override float ViewValue => Stacks;

        public BattleFocusBuff()
        {
        }

        public BattleFocusBuff(int stacks)
        {
            Stacks = stacks;
            Endless = true;
        }

        public override void OnAdded()
        {
            base.OnAdded();
            RebuildSubEffects();
        }

        public void SetStacks(int stacks)
        {
            if (stacks != Stacks)
            {
                Stacks = stacks;
                RebuildSubEffects();
            }
        }

        private void RebuildSubEffects()
        {
            ClearSubEffects();
            AddSubEffect("ranged_accuracy", PsyConfig.BattleFocusAccuracyPerStack * Stacks);
        }
    }

    /// <summary>First brush with death each raid: bonus AP and rapid pain decay for a few turns.</summary>
    public class AdrenalineRushBuff : PsyBuff
    {
        public AdrenalineRushBuff()
        {
        }

        public AdrenalineRushBuff(int duration)
            : base(duration)
        {
        }

        public override void OnAdded()
        {
            base.OnAdded();
            AddSubEffect("pain_threshold_regen", PsyConfig.AdrenalinePainRegen);
        }
    }

    /// <summary>Marker for the once-per-raid ignored pain stun; carries a short pain-regen boost.</summary>
    public class SecondWindBuff : PsyBuff
    {
        public SecondWindBuff()
        {
        }

        public SecondWindBuff(int duration)
            : base(duration)
        {
        }

        public override void OnAdded()
        {
            base.OnAdded();
            AddSubEffect("pain_threshold_regen", PsyConfig.AdrenalinePainRegen);
        }
    }

    /// <summary>Persistent scars, surfaced in the effects bar for the whole raid. Wound-effect
    /// penalties are built lazily on the first tick, when the creature link is guaranteed.</summary>
    public class ScarsEffect : PsyBuff
    {
        [Save]
        private bool _effectsBuilt;

        public ScarsEffect()
        {
            Endless = true;
        }

        public override float ViewValue => TraumaSystem.GetForCreature(_creature)?.Scars.Count ?? 0;

        public override EffectViewShowValueFormat ShowValueFormat => EffectViewShowValueFormat.Raw;

        public override bool IsRedView => true;

        public override void ProcessActionPoint()
        {
            base.ProcessActionPoint();
            if (_effectsBuilt || _creature == null)
            {
                return;
            }
            _effectsBuilt = true;
            MercPsyche mercPsyche = TraumaSystem.GetForCreature(_creature);
            if (mercPsyche == null)
            {
                return;
            }
            foreach (string scar in mercPsyche.Scars)
            {
                ScarDef scarDef = ScarCatalog.Get(scar);
                if (scarDef?.WoundEffects == null)
                {
                    continue;
                }
                foreach (KeyValuePair<string, float> woundEffect in scarDef.WoundEffects)
                {
                    AddSubEffect(woundEffect.Key, woundEffect.Value);
                }
            }
        }
    }

    /// <summary>Extracted at 75+ stress last raid: +2 Fortitude for this one. Display marker;
    /// the bonus itself is read from RaidState by StressSystem.</summary>
    public class SurvivorsHighBuff : PsyBuff
    {
        public SurvivorsHighBuff()
        {
            Endless = true;
        }

        public override float ViewValue => 2f;

        public override EffectViewShowValueFormat ShowValueFormat => EffectViewShowValueFormat.Raw;
    }

    /// <summary>High fortitude under Fear: the penalties are offset by sheer spite.</summary>
    public class GrimDeterminationBuff : PsyBuff
    {
        public GrimDeterminationBuff()
        {
        }

        public GrimDeterminationBuff(bool endless)
        {
            Endless = endless;
        }

        public override float ViewValue => PsyConfig.GrimDeterminationAccuracy;

        public override EffectViewShowValueFormat ShowValueFormat => EffectViewShowValueFormat.Percent100;

        public override void OnAdded()
        {
            base.OnAdded();
            AddSubEffect("melee_accuracy", PsyConfig.GrimDeterminationAccuracy);
            AddSubEffect("ranged_accuracy", PsyConfig.GrimDeterminationAccuracy);
        }
    }
}
