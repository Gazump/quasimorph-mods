using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MGSC;
using UnityEngine;

namespace CombatPsychology
{
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
            if (psyBuff is ScarsEffect scarsEffect)
            {
                BuildScarsTooltip(instance, scarsEffect);
                _createdTooltipField.SetValue(__instance, true);
                return false;
            }
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

        private static void BuildScarsTooltip(TooltipFactory instance, ScarsEffect scarsEffect)
        {
            MercPsyche mercPsyche = TraumaSystem.GetForCreature(EffectFields.GetCreature(scarsEffect));
            if (mercPsyche == null)
            {
                return;
            }
            instance.AddPanelToTooltip().SetMultilineName(Localization.Get("ui.psy.trauma") + ": " + mercPsyche.Trauma + "/" + TraumaSystem.TraumaMax).SetNameColor(Colors.White);
            foreach (string scar in mercPsyche.Scars)
            {
                ScarDef scarDef = ScarCatalog.Get(scar);
                if (scarDef != null)
                {
                    string text = Localization.Get("ui.psy.scar." + scar + ".name") + " — " + Localization.Get("ui.psy.scar." + scar + ".desc");
                    instance.AddPanelToTooltip().SetMultilineName(text).SetNameColor(scarDef.Positive ? Colors.AltGreen : Colors.LightRed);
                }
            }
            instance.AddPanelToTooltip().SetMultilineName(Localization.Get("ui.effect.ScarsEffect.desc")).SetNameColor(Colors.DarkYellow);
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
            else if (effect is ScarsEffect)
            {
                spriteName = "psy_scars_icon";
            }
            else if (effect is SurvivorsHighBuff)
            {
                spriteName = "psy_survivorshigh_icon";
            }
            else
            {
                spriteName = "psy_grimdetermination_icon";
            }
            return IconFactory.Get(spriteName, IconFactory.StatusIconReference);
        }
    }
}
