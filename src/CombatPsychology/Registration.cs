using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using MGSC;
using UnityEngine;

namespace CombatPsychology
{
    /// <summary>Registers all data-driven content: status effect records, the sedative item,
    /// vice-item stress relief, tooltip icons and localization rows.</summary>
    public static class Registration
    {
        public static void RegisterAll()
        {
            // Sample vanilla sprites first: every icon we create is PPU-normalized against
            // one of these so SetNativeSize/X100 renders it at vanilla size.
            IconFactory.StatusIconReference = FindStatusIconReference();
            _tooltipIconReference = FindTooltipIconReference();
            RegisterStressRecord();
            RegisterSedativeAddictionRecord();
            RegisterSedativeItem();
            PatchViceItems();
            RegisterTooltipIcons();
        }

        private static Sprite _tooltipIconReference;

        private static Sprite FindStatusIconReference()
        {
            foreach (StatusEffectsRecord record in Data.StatusEffects.Records)
            {
                if (record.ContentDescriptor is StatusEffectDescriptor statusEffectDescriptor && statusEffectDescriptor.StatusEffectIcon != null)
                {
                    return statusEffectDescriptor.StatusEffectIcon;
                }
            }
            Debug.LogWarning("[CombatPsychology] No vanilla status-effect icon found to sample scale from.");
            return null;
        }

        private static Sprite FindTooltipIconReference()
        {
            foreach (TooltipIconEntry entry in Data.TooltipIcons.Entries)
            {
                if (entry.Sprite != null)
                {
                    return entry.Sprite;
                }
            }
            Debug.LogWarning("[CombatPsychology] No vanilla tooltip icon found to sample scale from.");
            return null;
        }

        private static void RegisterStressRecord()
        {
            PsyStatusEffectsRecord record = new PsyStatusEffectsRecord(PsyConfig.StressId)
            {
                ProgressionType = StatusEffectProgressionType.Decrement,
                // Damage renewal: re-applying through the vanilla path (console command,
                // future stress-inflicting items/weapons) adds to the level instead of no-op.
                RenewalType = StatusEffectRenewalType.Damage,
                IncrementModifier = 1f,
                DecrementModifier = 1f,
                ResistModifier = 0f,
                HoldDurationMin = 0,
                HoldDurationMax = 0,
                VisualState = CreatureVisualState.None,
                BadLuckStep = 0f,
                BadLuckCap = 1f,
                Stage1WoundEffects = new Dictionary<string, float>(),
                Stage2WoundEffects = new Dictionary<string, float>
                {
                    { "accuracy_reduce", -0.1f },
                    { "income_pain", 0.15f }
                },
                // NOTE: fov_angle and dodge_reduce are fractions like accuracy_reduce
                // (the game multiplies by 100 for display and applies as (1 + value)).
                Stage3WoundEffects = new Dictionary<string, float>
                {
                    { "accuracy_reduce", -0.2f },
                    { "fov_angle", -0.25f },
                    { "dodge_reduce", -0.05f },
                    { "no_stealth", 1f }
                },
                Stage4WoundEffects = new Dictionary<string, float>
                {
                    { "accuracy_reduce", -0.3f },
                    { "income_pain", 0.3f },
                    { "hallucinations", 0.15f }
                },
                ContentDescriptor = CreateStatusEffectDescriptor("psy_stress_icon")
            };
            Data.StatusEffects.AddRecord(record.Id, record);
        }

        private static void RegisterSedativeAddictionRecord()
        {
            // The id's "Addiction" suffix makes the game treat it exactly like the vanilla
            // morphine/alcohol/nicotine addictions (achievements, perk immunity, stacking).
            PsyStatusEffectsRecord record = new PsyStatusEffectsRecord(PsyConfig.SedativeAddictionId)
            {
                ProgressionType = StatusEffectProgressionType.Increment,
                RenewalType = StatusEffectRenewalType.Damage,
                IncrementModifier = 1f,
                DecrementModifier = 1f,
                ResistModifier = 0f,
                HoldDurationMin = 0,
                HoldDurationMax = 0,
                VisualState = CreatureVisualState.None,
                BadLuckStep = 0f,
                BadLuckCap = 1f,
                Stage1WoundEffects = new Dictionary<string, float>(),
                Stage2WoundEffects = new Dictionary<string, float>
                {
                    { "income_pain", 0.1f }
                },
                Stage3WoundEffects = new Dictionary<string, float>
                {
                    { "accuracy_reduce", -0.05f },
                    { "income_pain", 0.2f }
                },
                Stage4WoundEffects = new Dictionary<string, float>
                {
                    { "accuracy_reduce", -0.1f },
                    { "income_pain", 0.3f },
                    { "pain_threshold_regen", -5f }
                },
                ContentDescriptor = CreateStatusEffectDescriptor("psy_sedative_addiction_icon")
            };
            Data.StatusEffects.AddRecord(record.Id, record);
        }

        private static void RegisterSedativeItem()
        {
            PsyConsumableRecord record = new PsyConsumableRecord(PsyConfig.SedativeItemId)
            {
                ItemClass = ItemClass.Pills,
                Categories = new List<string>(),
                TechLevel = 2,
                Price = 150f,
                Weight = 0.1f,
                InventoryWidthSize = 1,
                CanPutInVest = true,
                Disassembly = new List<ItemQuantity>(),
                MaxUsage = 1,
                UsageCost = 1,
                MaxStack = 5,
                StarvationValue = 0,
                HealthValue = 0,
                Duration = 0,
                MaxHealthValue = 0,
                QmorphosValue = 0,
                PainValue = -10,
                FixAllWoundsChance = 0f,
                HealAllWoundsChance = 0f,
                FixWeights = new Dictionary<string, float>(),
                Buffs = new Dictionary<string, float>(),
                EffectChance = new Dictionary<string, float>
                {
                    { PsyConfig.SedativeAddictionId, PsyConfig.SedativeAddictionChance }
                },
                EffectProgression = new Dictionary<string, int>
                {
                    { PsyConfig.StressId, PsyConfig.SedativeStressRelief }
                },
                ContentDescriptor = CreateSedativeDescriptor()
            };
            Data.Items.AddRecord(record.Id, record);
        }

        /// <summary>Alcohol calms nerves; anything carrying a nicotine addiction chance is a smoke.
        /// Both get an in-raid stress relief line (and tooltip) via EffectProgression.</summary>
        private static void PatchViceItems()
        {
            int num = 0;
            foreach (BasePickupItemRecord item in Data.Items.Records)
            {
                if (!(item is CompositeItemRecord compositeItemRecord))
                {
                    continue;
                }
                foreach (BasePickupItemRecord record in compositeItemRecord.Records)
                {
                    if (!(record is ConsumableRecord consumableRecord))
                    {
                        continue;
                    }
                    int stressRelief = 0;
                    if (consumableRecord.ItemClass == ItemClass.Alcohol)
                    {
                        stressRelief = PsyConfig.AlcoholStressRelief;
                    }
                    else if (consumableRecord.EffectChance != null && consumableRecord.EffectChance.ContainsKey("nicotineAddiction"))
                    {
                        stressRelief = PsyConfig.NicotineStressRelief;
                    }
                    if (stressRelief != 0)
                    {
                        if (consumableRecord.EffectProgression == null)
                        {
                            consumableRecord.EffectProgression = new Dictionary<string, int>();
                        }
                        if (!consumableRecord.EffectProgression.ContainsKey(PsyConfig.StressId))
                        {
                            consumableRecord.EffectProgression.Add(PsyConfig.StressId, stressRelief);
                            num++;
                        }
                    }
                }
            }
            Debug.Log($"[CombatPsychology] Added stress relief to {num} vice items.");
        }

        private static void RegisterTooltipIcons()
        {
            // Tags the game's generic tooltip builders will look up for our status effects.
            RegisterTooltipIcon("statuseffect_stress_buff");
            RegisterTooltipIcon("statuseffect_stress_debuff");
            RegisterTooltipIcon("statuseffect_stress_chance");
            RegisterTooltipIcon("statuseffect_sedativeAddiction_buff");
            RegisterTooltipIcon("statuseffect_sedativeAddiction_debuff");
            RegisterTooltipIcon("statuseffect_sedativeAddiction_chance");
        }

        private static void RegisterTooltipIcon(string tag)
        {
            Sprite sprite = IconFactory.Get(tag, _tooltipIconReference);
            Data.TooltipIcons.CreateOrRefreshEntry(sprite);
            Data.TooltipIcons.UpdateTag(sprite.name, tag);
        }

        private static StatusEffectDescriptor CreateStatusEffectDescriptor(string iconName)
        {
            StatusEffectDescriptor statusEffectDescriptor = ScriptableObject.CreateInstance<StatusEffectDescriptor>();
            FieldInfo field = typeof(StatusEffectDescriptor).GetField("_statusEffectIcon", BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(statusEffectDescriptor, IconFactory.Get(iconName, IconFactory.StatusIconReference));
            return statusEffectDescriptor;
        }

        /// <summary>Clones the descriptor of an existing pill-class item (keeping its render id,
        /// map icon and use-sound) and swaps in our inventory icon.</summary>
        private static ItemContentDescriptor CreateSedativeDescriptor()
        {
            ItemContentDescriptor template = null;
            foreach (BasePickupItemRecord item in Data.Items.Records)
            {
                if (item is CompositeItemRecord compositeItemRecord)
                {
                    foreach (BasePickupItemRecord record in compositeItemRecord.Records)
                    {
                        if (record is ConsumableRecord { ItemClass: ItemClass.Pills } consumableRecord && consumableRecord.ContentDescriptor is ItemContentDescriptor itemContentDescriptor && itemContentDescriptor.Icon != null)
                        {
                            template = itemContentDescriptor;
                            break;
                        }
                    }
                }
                if (template != null)
                {
                    break;
                }
            }
            ItemContentDescriptor descriptor;
            if (template != null)
            {
                descriptor = Object.Instantiate(template);
            }
            else
            {
                Debug.LogWarning("[CombatPsychology] No pill-class descriptor found to clone; sedative will use a bare descriptor.");
                descriptor = ScriptableObject.CreateInstance<ItemContentDescriptor>();
            }
            FieldInfo field = typeof(ItemContentDescriptor).GetField("_icon", BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(descriptor, IconFactory.Get("psy_sedative_item", template?.Icon));
            return descriptor;
        }

        // --- Localization -------------------------------------------------------------

        /// <summary>Rows appended to the game's tab-separated localization table.
        /// Column layout: key + 11 languages; English is replicated everywhere for v1.</summary>
        public static string BuildLocalizationRows()
        {
            StringBuilder stringBuilder = new StringBuilder();
            AddRow(stringBuilder, "ui.effect.stress.caption", "Stress");
            AddRow(stringBuilder, "ui.effect.stress.desc", "Combat stress is taking its toll. It fades slowly in quiet moments, escalating through anxiety, fear and terror if fresh horrors keep coming. Sedatives, alcohol and smoking bring it down.");
            AddRow(stringBuilder, "ui.effect.sedativeAddiction.caption", "Sedative addiction");
            AddRow(stringBuilder, "ui.effect.sedativeAddiction.desc", "Dependence on sedatives. Grows on its own once acquired; the craving amplifies pain and shakes the hands.");
            AddRow(stringBuilder, "tooltip.statuseffect_stress", "Stress");
            AddRow(stringBuilder, "tooltip.statuseffect_stress_chance", "Stress chance");
            AddRow(stringBuilder, "tooltip.statuseffect_sedativeAddiction", "Sedative addiction");
            AddRow(stringBuilder, "tooltip.statuseffect_sedativeAddiction_chance", "Sedative addiction chance");
            AddRow(stringBuilder, "item.qm_psy_sedative.name", "Tranq-Eze sedative");
            AddRow(stringBuilder, "item.qm_psy_sedative.shortdesc", "Vents combat stress. Habit-forming.");
            AddRow(stringBuilder, "ui.effect.BloodlustBuff.caption", "Bloodlust");
            AddRow(stringBuilder, "ui.effect.BloodlustBuff.desc", "The killing has momentum of its own. Melee strikes land truer and pain fades — but the comedown will be paid in stress.");
            AddRow(stringBuilder, "ui.effect.BattleFocusBuff.caption", "Battle focus");
            AddRow(stringBuilder, "ui.effect.BattleFocusBuff.desc", "Dealing damage without taking any sharpens aim. Broken the moment something hurts you.");
            AddRow(stringBuilder, "ui.effect.AdrenalineRushBuff.caption", "Adrenaline rush");
            AddRow(stringBuilder, "ui.effect.AdrenalineRushBuff.desc", "Near death, the body overrides the mind: extra action points now, pain washed away for a few turns.");
            AddRow(stringBuilder, "ui.effect.SecondWindBuff.caption", "Second wind");
            AddRow(stringBuilder, "ui.effect.SecondWindBuff.desc", "Gritted teeth carried you through what should have dropped you. Once per raid.");
            AddRow(stringBuilder, "ui.effect.GrimDeterminationBuff.caption", "Grim determination");
            AddRow(stringBuilder, "ui.effect.GrimDeterminationBuff.desc", "Fear, mastered. While terror grips lesser mercs, this one aims straighter.");
            AddRow(stringBuilder, "ui.psy.fortitude", "Fortitude");
            AddRow(stringBuilder, "ui.psy.stressgain", "stress gain");
            AddRow(stringBuilder, "ui.psy.stressonexpiry", "Stress when it expires");
            AddRow(stringBuilder, "ui.psy.trauma", "Trauma");
            AddRow(stringBuilder, "ui.effect.ScarsEffect.caption", "Psychological scars");
            AddRow(stringBuilder, "ui.effect.ScarsEffect.desc", "Old wounds of the mind ride along on every raid. Trauma accumulates from what a merc endures; treatment is the only way out.");
            AddRow(stringBuilder, "ui.effect.SurvivorsHighBuff.caption", "Survivor's high");
            AddRow(stringBuilder, "ui.effect.SurvivorsHighBuff.desc", "Walked out of hell last time and lived. Fortitude +2 for this raid.");
            AddRow(stringBuilder, "ui.psy.scar.shell_shock.name", "Shell shock");
            AddRow(stringBuilder, "ui.psy.scar.shell_shock.desc", "Starts raids at 20 stress; explosions are twice as stressful.");
            AddRow(stringBuilder, "ui.psy.scar.night_terrors.name", "Night terrors");
            AddRow(stringBuilder, "ui.psy.scar.night_terrors.desc", "Fortitude -1; stress never settles below 10 in the field.");
            AddRow(stringBuilder, "ui.psy.scar.depression.name", "Depression");
            AddRow(stringBuilder, "ui.psy.scar.depression.desc", "Fortitude -2; stress gain +25%; perk experience -25%.");
            AddRow(stringBuilder, "ui.psy.scar.substance_dependence.name", "Substance dependence");
            AddRow(stringBuilder, "ui.psy.scar.substance_dependence.desc", "Begins every raid already hooked on sedatives.");
            AddRow(stringBuilder, "ui.psy.scar.death_wish.name", "Death wish");
            AddRow(stringBuilder, "ui.psy.scar.death_wish.desc", "Breakdowns come easier and turn lethal twice as often; +10% damage dealt.");
            AddRow(stringBuilder, "ui.psy.scar.cold_blood.name", "Cold blood");
            AddRow(stringBuilder, "ui.psy.scar.cold_blood.desc", "Fortitude +1; stress gain -20%. Forged by surviving horror after horror, unbroken.");
            AddRow(stringBuilder, "ui.psy.evaluation", "Psychological evaluation");
            AddRow(stringBuilder, "ui.psy.evalhint", "Psychological evaluation");
            AddRow(stringBuilder, "ui.psy.cleanstreak", "Unbroken streak");
            AddRow(stringBuilder, "ui.psy.pendingnextraid", "active next raid");
            AddRow(stringBuilder, "ui.psy.noscars", "No psychological scars.");
            AddRow(stringBuilder, "ui.psy.newscar", "new scar:");
            AddRow(stringBuilder, "ui.psy.newpositivescar", "hardened:");
            AddRow(stringBuilder, "ui.psy.scars", "Psychological scars");
            return stringBuilder.ToString();
        }

        private static void AddRow(StringBuilder sb, string key, string english)
        {
            sb.Append('\n').Append(key);
            for (int i = 0; i < 11; i++)
            {
                sb.Append('\t').Append(english);
            }
        }
    }
}
