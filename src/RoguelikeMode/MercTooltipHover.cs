using MGSC;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RoguelikeMode
{
    public class MercTooltipHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Mercenary Merc;
        public MercenaryProfileRecord Profile;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Merc != null && Profile != null)
            {
                SingletonMonoBehaviour<TooltipFactory>.Instance.BuildMercenaryTooltip(Merc, Profile);
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
