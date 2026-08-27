using MGSC;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RoguelikeMode
{
    public class ClassTooltipHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public string ClassId;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (string.IsNullOrEmpty(ClassId))
            {
                return;
            }
            MercenaryClassRecord record = Data.MercenaryClasses.GetRecord(ClassId);
            if (record == null)
            {
                return;
            }
            TooltipFactory factory = SingletonMonoBehaviour<TooltipFactory>.Instance;
            PropertiesTooltip tooltip = factory.BuildEmptyTooltip(wide: true);
            tooltip.SetCaption1(Localization.Get("class." + ClassId + ".name"), factory.FirstLetterColor);
            tooltip.SetCaption2(Localization.Get("ui.dive.classcaption"));
            foreach (string perkId in record.PerkIds)
            {
                string perkTag = FormatHelper.ClearPerkGrades(perkId);
                factory.AddPanelToTooltip().SetMultilineName(Localization.Get("perk." + perkTag + ".name")).SetNameColor(Colors.DarkYellow);
                string description = FormatHelper.GetPerkDesc(perkId);
                if (!string.IsNullOrEmpty(description))
                {
                    factory.AddPanelToTooltip().SetMultilineName(description);
                }
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SingletonMonoBehaviour<TooltipFactory>.Instance.HideTooltip();
        }

        private void OnDisable()
        {
            if (SingletonMonoBehaviour<TooltipFactory>.Instance != null)
            {
                SingletonMonoBehaviour<TooltipFactory>.Instance.HideTooltip();
            }
        }
    }
}
