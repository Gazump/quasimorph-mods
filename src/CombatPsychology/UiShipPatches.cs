using System.Reflection;
using HarmonyLib;
using MGSC;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CombatPsychology
{
    internal static class PsycheReport
    {
        public static string MercName(string profileId)
        {
            return Localization.Get("spec." + profileId + ".name");
        }

        public static string ScarName(string scarId)
        {
            return Localization.Get("ui.psy.scar." + scarId + ".name");
        }

        public static void ShowEvaluationTooltip(Mercenary mercenary)
        {
            MercPsyche mercPsyche = PsycheStore.Current.Find(mercenary.ProfileId);
            TooltipFactory instance = SingletonMonoBehaviour<TooltipFactory>.Instance;
            PropertiesTooltip propertiesTooltip = instance.BuildEmptyTooltip();
            propertiesTooltip.SetCaption1(Localization.Get("ui.psy.evaluation"), Colors.White);
            propertiesTooltip.SetCaption2(MercName(mercenary.ProfileId));
            int trauma = mercPsyche?.Trauma ?? 0;
            instance.AddPanelToTooltip().SetIcon("statuseffect_stress_debuff")
                .SetName(Localization.Get("ui.psy.trauma"))
                .SetValue($"{trauma}/{TraumaSystem.TraumaMax}")
                .SetValueColor(Colors.LightRed);
            instance.AddPanelToTooltip().SetIcon("statuseffect_stress_chance")
                .SetName(Localization.Get("ui.psy.fortitude"))
                .SetValue(StressSystem.GetFortitudeForMerc(mercenary).ToString());
            if (mercPsyche != null)
            {
                if (mercPsyche.CleanRaidStreak > 0)
                {
                    instance.AddPanelToTooltip().SetName(Localization.Get("ui.psy.cleanstreak")).SetValue(mercPsyche.CleanRaidStreak.ToString());
                }
                if (mercPsyche.SurvivorsHighPending)
                {
                    instance.AddPanelToTooltip().SetMultilineName(Localization.Get("ui.effect.SurvivorsHighBuff.caption") + ": " + Localization.Get("ui.psy.pendingnextraid")).SetNameColor(Colors.AltGreen);
                }
            }
            if (mercPsyche == null || mercPsyche.Scars.Count == 0)
            {
                instance.AddPanelToTooltip().SetMultilineName(Localization.Get("ui.psy.noscars")).SetNameColor(Colors.Green);
                return;
            }
            foreach (string scar in mercPsyche.Scars)
            {
                ScarDef scarDef = ScarCatalog.Get(scar);
                if (scarDef != null)
                {
                    string text = ScarName(scar).WrapInColor(scarDef.Positive ? Colors.AltGreen : Colors.LightRed) + " — " + Localization.Get("ui.psy.scar." + scar + ".desc");
                    instance.AddPanelToTooltip().SetMultilineName(text);
                }
            }
        }
    }

    internal static class DebriefNotifier
    {
        public static void Notify()
        {
            RaidDebrief lastDebrief = TraumaSystem.LastDebrief;
            if (lastDebrief == null)
            {
                return;
            }
            try
            {
                string text = PsycheReport.MercName(lastDebrief.ProfileId).WrapInColor(Colors.Yellow);
                if (lastDebrief.Delta != 0)
                {
                    string arg = ((lastDebrief.Delta > 0) ? ("+" + lastDebrief.Delta) : lastDebrief.Delta.ToString());
                    UI.Staff.NotificationPanel.AddNotification($"{text}: {Localization.Get("ui.psy.trauma")} {arg} ({lastDebrief.TraumaAfter}/{TraumaSystem.TraumaMax})");
                }
                foreach (string newScar in lastDebrief.NewScars)
                {
                    ScarDef scarDef = ScarCatalog.Get(newScar);
                    string text2 = PsycheReport.ScarName(newScar);
                    UI.Staff.NotificationPanel.AddNotification(text + ": " + Localization.Get((scarDef != null && scarDef.Positive) ? "ui.psy.newpositivescar" : "ui.psy.newscar") + " " + text2.WrapInColor((scarDef != null && scarDef.Positive) ? Colors.AltGreen : Colors.LightRed));
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[CombatPsychology] Could not queue trauma notification: " + ex.Message);
            }
        }
    }

    [HarmonyPatch(typeof(AfterRaidStatisticWindow), nameof(AfterRaidStatisticWindow.Configure))]
    internal static class AfterRaidStatisticWindow_Patch
    {
        private static readonly MethodInfo _addPanelToBlock = AccessTools.Method(typeof(AfterRaidStatisticWindow), "AddPanelToBlock");
        private static readonly FieldInfo _beneficiaryContent = AccessTools.Field(typeof(AfterRaidStatisticWindow), "_benificiaryContent");

        private static void Postfix(AfterRaidStatisticWindow __instance, Mercenary mercenary)
        {
            RaidDebrief lastDebrief = TraumaSystem.LastDebrief;
            if (lastDebrief == null || lastDebrief.ProfileId != mercenary.ProfileId)
            {
                return;
            }
            try
            {
                RectTransform rect = (RectTransform)_beneficiaryContent.GetValue(__instance);
                if (lastDebrief.Delta != 0)
                {
                    bool relief = lastDebrief.Delta < 0;
                    TooltipProperty tooltipProperty = (TooltipProperty)_addPanelToBlock.Invoke(__instance, new object[] { rect });
                    tooltipProperty.SetIcon(relief ? "statuseffect_stress_buff" : "statuseffect_stress_debuff")
                        .SetName(Localization.Get("ui.psy.trauma") + $" ({lastDebrief.TraumaAfter}/{TraumaSystem.TraumaMax})")
                        .SetValue((lastDebrief.Delta > 0) ? ("+" + lastDebrief.Delta) : lastDebrief.Delta.ToString())
                        .SetNameColor(relief ? Colors.AltGreen : Colors.LightRed);
                }
                foreach (string newScar in lastDebrief.NewScars)
                {
                    ScarDef scarDef = ScarCatalog.Get(newScar);
                    TooltipProperty tooltipProperty2 = (TooltipProperty)_addPanelToBlock.Invoke(__instance, new object[] { rect });
                    tooltipProperty2.SetIcon("statuseffect_stress_chance")
                        .SetName(Localization.Get((scarDef != null && scarDef.Positive) ? "ui.psy.newpositivescar" : "ui.psy.newscar") + " " + PsycheReport.ScarName(newScar))
                        .SetNameColor((scarDef != null && scarDef.Positive) ? Colors.AltGreen : Colors.LightRed);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[CombatPsychology] Could not add trauma rows to raid statistics: " + ex.Message);
            }
        }
    }

    [HarmonyPatch(typeof(TooltipFactory), nameof(TooltipFactory.BuildMercenaryTooltip))]
    internal static class MercTooltip_Patch
    {
        private static void Postfix(TooltipFactory __instance, Mercenary mercenary)
        {
            int fortitudeForMerc = StressSystem.GetFortitudeForMerc(mercenary);
            MercPsyche mercPsyche = PsycheStore.Current.Find(mercenary.ProfileId);
            TooltipProperty tooltipProperty = __instance.AddPanelToTooltip().SetIcon("statuseffect_stress_chance")
                .SetName(Localization.Get("ui.psy.fortitude"))
                .SetValue(fortitudeForMerc.ToString());
            if (mercPsyche != null && mercPsyche.Scars.Count > 0)
            {
                tooltipProperty.SetValueColor(Colors.Yellow);
                __instance.AddPanelToTooltip().SetIcon("statuseffect_stress_debuff")
                    .SetName(Localization.Get("ui.psy.scars"))
                    .SetValue(mercPsyche.Scars.Count.ToString())
                    .SetNameColor(Colors.LightRed);
            }
        }
    }

    public class PsycheIconButton : MonoBehaviour, IPointerClickHandler, IEventSystemHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public Mercenary Mercenary;
        public Image SelectionBorder;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (Mercenary != null)
            {
                PsycheReport.ShowEvaluationTooltip(Mercenary);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (SelectionBorder != null)
            {
                SelectionBorder.gameObject.SetActive(value: true);
            }
            if (Mercenary != null)
            {
                PsycheReport.ShowEvaluationTooltip(Mercenary);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (SelectionBorder != null)
            {
                SelectionBorder.gameObject.SetActive(value: false);
            }
            SingletonMonoBehaviour<TooltipFactory>.Instance.HideTooltip();
        }

        private void OnDisable()
        {
            OnPointerExit(null);
        }
    }

    [HarmonyPatch(typeof(MercenaryPanel), nameof(MercenaryPanel.Initialize))]
    internal static class MercenaryPanel_Patch
    {
        private const string CloneName = "CP_PsycheIcon";
        private static readonly FieldInfo _classIconField = AccessTools.Field(typeof(MercenaryPanel), "_classIcon");
        private static readonly FieldInfo _iconField = AccessTools.Field(typeof(MercenaryClassIcon), "_icon");
        private static readonly FieldInfo _borderField = AccessTools.Field(typeof(MercenaryClassIcon), "_selectionBorder");

        private static void Postfix(MercenaryPanel __instance, Mercenary mercenary, MercenaryPanel.Mode mode)
        {
            try
            {
                MercenaryClassIcon mercenaryClassIcon = (MercenaryClassIcon)_classIconField.GetValue(__instance);
                if (mercenaryClassIcon == null)
                {
                    return;
                }
                Transform parent = mercenaryClassIcon.transform.parent;
                Transform existing = parent.Find(CloneName);
                if (existing != null)
                {
                    PsycheIconButton component = existing.GetComponent<PsycheIconButton>();
                    if (component != null)
                    {
                        component.Mercenary = mercenary;
                    }
                    existing.gameObject.SetActive(value: true);
                    return;
                }
                GameObject clone = Object.Instantiate(mercenaryClassIcon.gameObject, parent, worldPositionStays: false);
                clone.name = CloneName;
                MercenaryClassIcon cloneStock = clone.GetComponent<MercenaryClassIcon>();
                Image image = (Image)_iconField.GetValue(cloneStock);
                Image border = (Image)_borderField.GetValue(cloneStock);
                Object.Destroy(cloneStock);
                if (image != null)
                {
                    image.sprite = IconFactory.Get("psy_brain_icon");
                }
                if (border != null)
                {
                    border.gameObject.SetActive(value: false);
                }
                RectTransform classRect = (RectTransform)mercenaryClassIcon.transform;
                RectTransform cloneRect = (RectTransform)clone.transform;
                LayoutElement layoutElement = clone.AddComponent<LayoutElement>();
                layoutElement.ignoreLayout = true;
                Vector2 size = classRect.rect.size;
                if (size.x < 1f || size.y < 1f)
                {
                    size = classRect.sizeDelta;
                }
                cloneRect.anchorMin = new Vector2(0f, 0.5f);
                cloneRect.anchorMax = new Vector2(0f, 0.5f);
                cloneRect.pivot = new Vector2(1f, 0.5f);
                if (size.x >= 1f && size.y >= 1f)
                {
                    cloneRect.sizeDelta = size;
                }
                cloneRect.anchoredPosition = new Vector2(-8f, 0f);
                PsycheIconButton psycheIconButton = clone.AddComponent<PsycheIconButton>();
                psycheIconButton.Mercenary = mercenary;
                psycheIconButton.SelectionBorder = border;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[CombatPsychology] Could not add psyche button to mercenary panel: " + ex.Message);
            }
        }
    }
}
