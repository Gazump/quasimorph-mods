using System;
using System.Collections.Generic;
using MGSC;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoguelikeMode
{
    public class DiveClock : MonoBehaviour
    {
        public TextMeshProUGUI Label;

        private int _lastSecond = -1;
        private string _day = RogueRun.TodayLabel();

        private void Update()
        {
            if (Label == null)
            {
                return;
            }
            DateTime now = DateTime.UtcNow;
            if (now.Second == _lastSecond)
            {
                return;
            }
            _lastSecond = now.Second;
            TimeSpan left = now.Date.AddDays(1) - now;
            Label.text = $"DAILY RESETS IN {(int)left.TotalHours:00}:{left.Minutes:00}:{left.Seconds:00}";
            string today = RogueRun.TodayLabel();
            if (_day != today)
            {
                _day = today;
                DiveScreen.OnDayRollover();
            }
        }
    }

    public static class DiveScreen
    {
        private static GameObject _root;
        private static State _state;
        private static CommonButton _sourceButton;
        private static Transform _operatorSection;
        private static TextMeshProUGUI _logText;

        private static bool _daily = true;
        private static RogueTier _tier = RogueTier.Normal;
        private static int _candidate;

        private static readonly List<(CommonButton button, bool daily)> _modeButtons = new List<(CommonButton, bool)>();
        private static readonly List<(CommonButton button, RogueTier tier)> _tierButtons = new List<(CommonButton, RogueTier)>();
        private static readonly List<CommonButton> _operatorButtons = new List<CommonButton>();

        public static void Open(State state, CommonButton sourceButton)
        {
            _state = state;
            if (sourceButton != null)
            {
                _sourceButton = sourceButton;
            }
            if (_sourceButton == null)
            {
                return;
            }
            Close();
            _daily = true;
            _tier = RogueTier.Normal;
            _candidate = 0;
            RogueRun.PrepareDay(daily: true);
            Build();
            RefreshOperators();
            RefreshSelections();
            RefreshLog();
        }

        public static void Close()
        {
            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
                _root = null;
            }
            _modeButtons.Clear();
            _tierButtons.Clear();
            _operatorButtons.Clear();
        }

        private static void Build()
        {
            MainMenuScreen screen = UI.Get<MainMenuScreen>();
            _root = new GameObject("DiveScreen", typeof(RectTransform));
            RectTransform rootRect = _root.transform as RectTransform;
            rootRect.SetParent(screen.transform, worldPositionStays: false);
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            rootRect.SetAsLastSibling();

            Image background = _root.AddComponent<Image>();
            background.color = new Color(0.01f, 0.035f, 0.015f, 0.985f);

            AddLabel(_root.transform, "ui.dive.title", "THE DIVE", 0.04f, 0.92f, 0.6f, 0.98f, 34f, TextAlignmentOptions.Left);
            LocalizationInjector.Set("ui.dive.day", RogueRun.Daily ? ("DAILY: " + RogueRun.DayLabel) : "RANDOM DIVE");
            AddLabel(_root.transform, "ui.dive.day", null, 0.6f, 0.93f, 0.96f, 0.98f, 22f, TextAlignmentOptions.Right);
            GameObject clockLabel = CloneLabel(_root.transform);
            RectTransform clockRect = clockLabel.transform as RectTransform;
            clockRect.anchorMin = new Vector2(0.6f, 0.895f);
            clockRect.anchorMax = new Vector2(0.96f, 0.93f);
            clockRect.offsetMin = Vector2.zero;
            clockRect.offsetMax = Vector2.zero;
            TextMeshProUGUI clockText = clockLabel.GetComponent<TextMeshProUGUI>();
            clockText.enableAutoSizing = false;
            clockText.alignment = TextAlignmentOptions.Right;
            clockText.fontSize = clockText.fontSize * 0.75f;
            UnityEngine.Object.Destroy(clockLabel.GetComponent<LocalizableLabel>());
            DiveClock clock = _root.AddComponent<DiveClock>();
            clock.Label = clockText;

            RectTransform leftPanel = AddPanel(0.04f, 0.06f, 0.40f, 0.90f);
            VerticalLayoutGroup leftLayout = leftPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            leftLayout.padding = new RectOffset(20, 20, 18, 18);
            leftLayout.spacing = 6f;
            leftLayout.childControlWidth = true;
            leftLayout.childControlHeight = true;
            leftLayout.childForceExpandWidth = true;
            leftLayout.childForceExpandHeight = false;

            RectTransform rightPanel = AddPanel(0.44f, 0.06f, 0.96f, 0.90f);
            VerticalLayoutGroup rightLayout = rightPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            rightLayout.padding = new RectOffset(24, 24, 18, 18);
            rightLayout.spacing = 8f;
            rightLayout.childControlWidth = true;
            rightLayout.childControlHeight = true;
            rightLayout.childForceExpandWidth = true;
            rightLayout.childForceExpandHeight = false;

            AddSectionHeader(leftPanel, "ui.dive.mode", "MODE");
            _modeButtons.Add((AddRowButton(leftPanel, "ui.dive.optdaily", () => SelectMode(true)), true));
            _modeButtons.Add((AddRowButton(leftPanel, "ui.dive.optrandom", () => SelectMode(false)), false));

            AddSectionHeader(leftPanel, "ui.dive.difficulty", "DIFFICULTY");
            _tierButtons.Add((AddRowButton(leftPanel, "ui.dive.opteasy", () => SelectTier(RogueTier.Easy)), RogueTier.Easy));
            _tierButtons.Add((AddRowButton(leftPanel, "ui.dive.optnormal", () => SelectTier(RogueTier.Normal)), RogueTier.Normal));
            _tierButtons.Add((AddRowButton(leftPanel, "ui.dive.opthard", () => SelectTier(RogueTier.Hard)), RogueTier.Hard));

            AddSectionHeader(leftPanel, "ui.dive.operator", "OPERATOR");
            _operatorSection = leftPanel;
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                _operatorButtons.Add(AddRowButton(leftPanel, "ui.dive.merc" + i, () => SelectOperator(index)));
            }

            AddSectionHeader(leftPanel, "ui.dive.spacer", " ");
            CommonButton start = AddRowButton(leftPanel, "ui.dive.start", StartDive);
            LocalizationInjector.Set("ui.dive.start", "START DIVE");
            start.ChangeLabel("ui.dive.start");
            if (RunPersistence.HasSave())
            {
                AddRowButton(leftPanel, RogueConfig.ResumeCaptionKey, ResumeDive);
            }
            AddRowButton(leftPanel, "ui.dive.close", CloseClicked);
            LocalizationInjector.Set("ui.dive.close", "< CLOSE");

            TextMeshProUGUI logHeader = AddSectionHeader(rightPanel, "ui.dive.logheader", "DIVE LOG");
            GameObject logObject = CloneLabel(rightPanel);
            _logText = logObject.GetComponent<TextMeshProUGUI>();
            LayoutElement logElement = logObject.AddComponent<LayoutElement>();
            logElement.flexibleHeight = 1f;
            _logText.enableAutoSizing = false;
            _logText.enableWordWrapping = true;
            _logText.overflowMode = TextOverflowModes.Truncate;
            _logText.alignment = TextAlignmentOptions.TopLeft;
            _logText.fontSize = logHeader.fontSize * 0.9f;
            _logText.lineSpacing = 8f;
            UnityEngine.Object.Destroy(logObject.GetComponent<LocalizableLabel>());
        }

        private static RectTransform AddPanel(float xMin, float yMin, float xMax, float yMax)
        {
            GameObject visual = UnityEngine.Object.Instantiate(_sourceButton.gameObject, _root.transform);
            visual.name = "DivePanelChrome";
            RectTransform visualRect = visual.transform as RectTransform;
            visualRect.anchorMin = new Vector2(xMin, yMin);
            visualRect.anchorMax = new Vector2(xMax, yMax);
            visualRect.pivot = new Vector2(0.5f, 0.5f);
            visualRect.offsetMin = Vector2.zero;
            visualRect.offsetMax = Vector2.zero;
            CommonButton chromeButton = visual.GetComponent<CommonButton>();
            if (chromeButton != null && chromeButton.CaptionLabel != null)
            {
                UnityEngine.Object.Destroy(chromeButton.CaptionLabel.gameObject);
            }
            if (chromeButton != null)
            {
                UnityEngine.Object.Destroy(chromeButton);
            }
            LayoutElement chromeElement = visual.GetComponent<LayoutElement>();
            if (chromeElement != null)
            {
                UnityEngine.Object.Destroy(chromeElement);
            }
            GameObject content = new GameObject("DivePanelContent", typeof(RectTransform));
            RectTransform rect = content.transform as RectTransform;
            rect.SetParent(_root.transform, worldPositionStays: false);
            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMax, yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static GameObject CloneLabel(Transform parent)
        {
            GameObject label = UnityEngine.Object.Instantiate(_sourceButton.CaptionLabel.gameObject, parent);
            label.name = "DiveLabel";
            return label;
        }

        private static void AddLabel(Transform parent, string key, string english, float xMin, float yMin, float xMax, float yMax, float fontSize, TextAlignmentOptions alignment)
        {
            if (english != null)
            {
                LocalizationInjector.Set(key, english);
            }
            GameObject label = CloneLabel(parent);
            RectTransform rect = label.transform as RectTransform;
            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMax, yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            TextMeshProUGUI text = label.GetComponent<TextMeshProUGUI>();
            text.enableAutoSizing = false;
            text.alignment = alignment;
            text.fontSize = fontSize;
            label.GetComponent<LocalizableLabel>().ChangeLabel(key);
        }

        private static float _uiFontSize = 18f;

        private static TextMeshProUGUI AddSectionHeader(Transform parent, string key, string english)
        {
            LocalizationInjector.Set(key, english);
            GameObject label = CloneLabel(parent);
            TextMeshProUGUI text = label.GetComponent<TextMeshProUGUI>();
            text.enableAutoSizing = false;
            text.alignment = TextAlignmentOptions.Left;
            label.GetComponent<LocalizableLabel>().ChangeLabel(key);
            _uiFontSize = text.fontSize;
            LayoutElement element = label.AddComponent<LayoutElement>();
            element.preferredHeight = text.fontSize * 1.5f;
            return text;
        }

        private static CommonButton AddRowButton(Transform parent, string captionKey, Action onClick)
        {
            CommonButton button = UnityEngine.Object.Instantiate(_sourceButton, parent);
            button.gameObject.name = "DiveButton_" + captionKey;
            LayoutElement element = button.gameObject.GetComponent<LayoutElement>();
            if (element == null)
            {
                element = button.gameObject.AddComponent<LayoutElement>();
            }
            element.ignoreLayout = false;
            button.ChangeLabel(captionKey);
            TextMeshProUGUI text = button.CaptionLabel != null ? button.CaptionLabel.Text : null;
            float captionSize = _uiFontSize;
            if (text != null)
            {
                text.enableWordWrapping = false;
                text.overflowMode = TextOverflowModes.Ellipsis;
                captionSize = text.fontSize;
            }
            element.preferredHeight = Mathf.Max(captionSize * 2.2f, 30f);
            button.OnClick += (b, clicks) => onClick();
            return button;
        }

        private static void SelectMode(bool daily)
        {
            _daily = daily;
            _candidate = 0;
            RogueRun.PrepareDay(daily);
            LocalizationInjector.Set("ui.dive.day", daily ? ("DAILY: " + RogueRun.DayLabel) : "RANDOM DIVE");
            RefreshOperators();
            RefreshSelections();
        }

        private static void SelectTier(RogueTier tier)
        {
            _tier = tier;
            RefreshSelections();
        }

        private static void SelectOperator(int index)
        {
            _candidate = index;
            RefreshSelections();
        }

        private static void RefreshOperators()
        {
            List<Mercenary> candidates = RogueRunner.GetCandidateMercs(_state, forceRebuild: true);
            for (int i = 0; i < _operatorButtons.Count; i++)
            {
                if (i >= candidates.Count)
                {
                    _operatorButtons[i].gameObject.SetActive(false);
                    continue;
                }
                Mercenary mercenary = candidates[i];
                _operatorButtons[i].gameObject.SetActive(true);
                MercTooltipHover hover = _operatorButtons[i].gameObject.GetComponent<MercTooltipHover>();
                if (hover == null)
                {
                    hover = _operatorButtons[i].gameObject.AddComponent<MercTooltipHover>();
                }
                hover.Merc = mercenary;
                hover.Profile = Data.MercenaryProfiles.GetRecord(mercenary.ProfileId);
            }
        }

        private static void RefreshSelections()
        {
            LocalizationInjector.Set("ui.dive.optdaily", Caption("DAILY DIVE", _daily));
            LocalizationInjector.Set("ui.dive.optrandom", Caption("RANDOM DIVE", !_daily));
            foreach ((CommonButton button, bool daily) in _modeButtons)
            {
                button.ChangeLabel(daily ? "ui.dive.optdaily" : "ui.dive.optrandom");
            }
            LocalizationInjector.Set("ui.dive.opteasy", Caption("EASY", _tier == RogueTier.Easy));
            LocalizationInjector.Set("ui.dive.optnormal", Caption("NORMAL", _tier == RogueTier.Normal));
            LocalizationInjector.Set("ui.dive.opthard", Caption("HARD", _tier == RogueTier.Hard));
            foreach ((CommonButton button, RogueTier tier) in _tierButtons)
            {
                button.ChangeLabel(tier == RogueTier.Easy ? "ui.dive.opteasy" : (tier == RogueTier.Normal ? "ui.dive.optnormal" : "ui.dive.opthard"));
            }
            List<Mercenary> candidates = RogueRunner.GetCandidateMercs(_state);
            for (int i = 0; i < _operatorButtons.Count && i < candidates.Count; i++)
            {
                string name = Localization.Get("spec." + candidates[i].ProfileId + ".name");
                string className = Localization.Get("class." + candidates[i].MercClassId + ".name");
                LocalizationInjector.Set("ui.dive.merc" + i, Caption(name + " - " + className, _candidate == i));
                _operatorButtons[i].ChangeLabel("ui.dive.merc" + i);
            }
        }

        private static string Caption(string text, bool selected)
        {
            return selected ? ("> " + text) : text;
        }

        private static void RefreshLog()
        {
            if (_logText == null)
            {
                return;
            }
            string text = string.Empty;
            if (!string.IsNullOrEmpty(RogueRun.LastSummary))
            {
                text += "LAST DIVE\n" + RogueRun.LastSummary + "\n\n";
            }
            RogueScoreStore store = ScoreSystem.Load();
            if (store.Entries.Count > 0)
            {
                text += "TOP DIVES\n";
                int count = Mathf.Min(8, store.Entries.Count);
                for (int i = 0; i < count; i++)
                {
                    RogueScoreEntry e = store.Entries[i];
                    string mode = e.Daily ? e.Day : "random";
                    string name = Localization.Get("spec." + e.ProfileId + ".name");
                    text += $"{i + 1}. {e.Score} pts - floor {e.Floor}, {e.Kills} kills, {(RogueTier)e.Tier}, {mode}, {name}{(e.Victory ? " - VICTORY" : "")}\n";
                }
            }
            if (string.IsNullOrEmpty(text))
            {
                text = $"No dives recorded yet.\n\nPick a mode, difficulty and operator, then START DIVE.\n\nReach floor {RogueConfig.FloorCount}, seize the GOLDEN KEYCARD, evacuate alive.";
            }
            _logText.text = text;
        }

        private static void StartDive()
        {
            if (RogueRun.Active)
            {
                return;
            }
            int candidate = _candidate;
            RogueTier tier = _tier;
            Close();
            RogueRunner.Get(_state).BeginRun(candidate, tier);
        }

        private static void ResumeDive()
        {
            if (RogueRun.Active)
            {
                return;
            }
            Close();
            RogueRunner.Get(_state).ResumeRun();
        }

        private static void CloseClicked()
        {
            Close();
        }

        public static void OnDayRollover()
        {
            if (_root == null || RogueRun.Active)
            {
                return;
            }
            Open(_state, null);
        }
    }
}
