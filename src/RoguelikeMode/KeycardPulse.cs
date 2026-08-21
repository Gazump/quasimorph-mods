using HarmonyLib;
using MGSC;
using UnityEngine;
using UnityEngine.UI;

namespace RoguelikeMode
{
    public class KeycardPulse : MonoBehaviour
    {
        public static Sprite[] IconFrames;

        private Image _image;
        private int _frame;
        private float _elapsed;

        public static void Attach(ItemSlot slot, BasePickupItem item)
        {
            Image icon = AccessTools.Field(typeof(ItemSlot), "_icon").GetValue(slot) as Image;
            if (icon == null)
            {
                return;
            }
            KeycardPulse pulse = icon.GetComponent<KeycardPulse>();
            bool wanted = item != null && item.Id == RogueConfig.KeycardId && IconFrames != null && IconFrames.Length > 1;
            if (!wanted)
            {
                if (pulse != null)
                {
                    pulse.enabled = false;
                }
                return;
            }
            if (pulse == null)
            {
                pulse = icon.gameObject.AddComponent<KeycardPulse>();
            }
            pulse._image = icon;
            pulse._frame = 0;
            pulse._elapsed = 0f;
            pulse.enabled = true;
        }

        public static void Detach(ItemSlot slot)
        {
            Image icon = AccessTools.Field(typeof(ItemSlot), "_icon").GetValue(slot) as Image;
            KeycardPulse pulse = icon != null ? icon.GetComponent<KeycardPulse>() : null;
            if (pulse != null)
            {
                pulse.enabled = false;
            }
        }

        private void Update()
        {
            if (_image == null || IconFrames == null || IconFrames.Length < 2)
            {
                enabled = false;
                return;
            }
            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed < 0.25f)
            {
                return;
            }
            _elapsed -= 0.25f;
            _frame = (_frame + 1) % IconFrames.Length;
            _image.sprite = IconFrames[_frame];
        }
    }

    [HarmonyPatch(typeof(ItemSlot), "Initialize")]
    public static class KeycardSlotInitPatch
    {
        public static void Postfix(ItemSlot __instance, BasePickupItem item)
        {
            KeycardPulse.Attach(__instance, item);
        }
    }

    [HarmonyPatch(typeof(ItemSlot), "InitializeEmpty")]
    public static class KeycardSlotEmptyPatch
    {
        public static void Postfix(ItemSlot __instance)
        {
            KeycardPulse.Detach(__instance);
        }
    }
}
