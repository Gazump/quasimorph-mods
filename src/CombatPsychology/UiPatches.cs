using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MGSC;
using UnityEngine;

namespace CombatPsychology
{
    /// <summary>
    /// The effects bar (EffectsView.CreatePanel) and the hover tooltip
    /// (CommonEffectPanel.InitTooltip) both dispatch on hardcoded concrete types, so this
    /// mod's buffs need a prefix on each to get an icon and a tooltip.
    /// </summary>
    [HarmonyPatch(typeof(EffectsView), "CreatePanel")]
    internal static class EffectsView_CreatePanel_Patch
    {
        private static readonly MethodInfo _getPanel = AccessTools.Method(typeof(EffectsView), "GetPanel");
        private static readonly FieldInfo _creaturesField = AccessTools.Field(typeof(EffectsView), "_creatures");

        private static bool Prefix(EffectsView __instance, BaseEffect effect, IEffectWithView effectWithView, ref CommonEffectPanel __result)
        {
            if (!(effect is PsyBuff))
            {
                return true;
            }
            CommonEffectPanel commonEffectPanel = (CommonEffectPanel)_getPanel.Invoke(__instance, null);
            Creatures creatures = (Creatures)_creaturesField.GetValue(__instance);
            commonEffectPanel.Initialize(creatures, effectWithView, BuffIcons.For(effect));
            commonEffectPanel.gameObject.SetActive(effectWithView.Show);
            __result = commonEffectPanel;
            return false;
        }
    }

    [HarmonyPatch(typeof(CommonEffectPanel), "InitTooltip")]
    internal static class CommonEffectPanel_InitTooltip_Patch
    {
        private static readonly FieldInfo _effectWithViewsField = AccessTools.Field(typeof(CommonEffectPanel), "_effectWithViews");
        private static readonly FieldInfo _createdTooltipField = AccessTools.Field(typeof(CommonEffectPanel), "_createdTooltip");

        private static bool Prefix(CommonEffectPanel __instance)
        {
            List<IEffectWithView> list = (List<IEffectWithView>)_effectWithViewsField.GetValue(__instance);
            if (list == null || list.Count == 0 || !(list[0] is PsyBuff psyBuff))
            {
                return true;
            }
            TooltipFactory instance = SingletonMonoBehaviour<TooltipFactory>.Instance;
            PropertiesTooltip propertiesTooltip = instance.BuildEmptyTooltip(wide: false, psyBuff.IsRedView);
            propertiesTooltip.SetCaption1(Localization.Get("ui.effect." + psyBuff.BuffId + ".caption"), Colors.White);
            propertiesTooltip.SetCaption2(Localization.Get("ui.label.effect"));
            // Same stat-line renderer the stress tooltip uses, fed from this buff's live
            // sub-effects; white value color since these are bonuses, not penalties.
            foreach (WoundEffect item in psyBuff.ResolveSubEffects())
            {
                instance.AddWoundEffectProperty(item.EffectId, item.ViewValue).SetValueColor(Colors.White);
            }
            if (psyBuff is BloodlustBuff)
            {
                instance.AddPanelToTooltip().SetIcon("statuseffect_stress_debuff").LocalizeName("ui.psy.stressonexpiry")
                    .SetValue("+" + PsyConfig.StressBloodlustComedown + "%")
                    .SetTextColor(Colors.LightRed);
            }
            instance.AddPanelToTooltip().SetMultilineName(Localization.Get("ui.effect." + psyBuff.BuffId + ".desc")).SetNameColor(Colors.DarkYellow);
            _createdTooltipField.SetValue(__instance, true);
            return false;
        }
    }

    internal static class BuffIcons
    {
        public static Sprite For(BaseEffect effect)
        {
            string spriteName;
            if (effect is BloodlustBuff)
            {
                spriteName = "psy_bloodlust_icon";
            }
            else if (effect is BattleFocusBuff)
            {
                spriteName = "psy_battlefocus_icon";
            }
            else if (effect is AdrenalineRushBuff)
            {
                spriteName = "psy_adrenaline_icon";
            }
            else if (effect is SecondWindBuff)
            {
                spriteName = "psy_secondwind_icon";
            }
            else
            {
                spriteName = "psy_grimdetermination_icon";
            }
            return IconFactory.Get(spriteName, IconFactory.StatusIconReference);
        }
    }
}
