using HarmonyLib;
using MGSC;
using UnityEngine;
using UnityEngine.UI;

namespace RoguelikeMode
{
    public static class MenuInjection
    {
        private const string ButtonName = "DiveMenuButton";

        private static CommonButton _source;
        private static State _state;

        public static void Inject(State state)
        {
            _state = state;
            MainMenuScreen screen = UI.Get<MainMenuScreen>();
            if (screen == null)
            {
                return;
            }
            _source = AccessTools.Field(typeof(MainMenuScreen), "_startGameBtn").GetValue(screen) as CommonButton;
            CommonButton settings = AccessTools.Field(typeof(MainMenuScreen), "_settingsBtn").GetValue(screen) as CommonButton;
            if (_source == null)
            {
                return;
            }
            Transform parent = _source.transform.parent;
            if (parent.Find(ButtonName) != null)
            {
                return;
            }
            RectTransform sourceRect = _source.transform as RectTransform;
            float rowStep = sourceRect.rect.height + 8f;
            if (settings != null)
            {
                float vanillaStep = Mathf.Abs(sourceRect.anchoredPosition.y - (settings.transform as RectTransform).anchoredPosition.y);
                if (vanillaStep > 1f)
                {
                    rowStep = vanillaStep;
                }
            }
            CommonButton button = Object.Instantiate(_source, parent);
            button.gameObject.name = ButtonName;
            RectTransform buttonRect = button.transform as RectTransform;
            LayoutElement layoutElement = button.gameObject.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = button.gameObject.AddComponent<LayoutElement>();
            }
            layoutElement.ignoreLayout = true;
            buttonRect.anchorMin = sourceRect.anchorMin;
            buttonRect.anchorMax = sourceRect.anchorMax;
            buttonRect.pivot = sourceRect.pivot;
            buttonRect.sizeDelta = sourceRect.sizeDelta;
            buttonRect.anchoredPosition = sourceRect.anchoredPosition + new Vector2(sourceRect.rect.width + 24f, 0f);
            button.ChangeLabel(RogueConfig.DiveButtonCaptionKey);
            button.OnClick += (b, clicks) => OpenDive();
        }

        public static void OpenDive()
        {
            if (!RogueRun.Active)
            {
                DiveScreen.Open(_state, _source);
            }
        }
    }
}
