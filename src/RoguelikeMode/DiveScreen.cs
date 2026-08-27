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
        private static TextMeshProUGUI _ladderText;
        private static TextMeshProUGUI _rightHeader;
        private static CommonButton _ladderToggle;
        private static int _rightTab;
        private static string _fetchedKey = string.Empty;

        private static bool _daily = true;
        private static RogueTier _tier = RogueTier.Normal;
        private static int _candidate;
        private static int _lengthIndex = 1;
        private static CommonButton _lengthButton;

        private static readonly List<(CommonButton button, bool daily)> _modeButtons = new List<(CommonButton, bool)>();
        private static readonly List<(CommonButton button, RogueTier tier)> _tierButtons = new List<(CommonButton, RogueTier)>();
        private static readonly List<CommonButton> _operatorButtons = new List<CommonButton>();
        private static readonly List<(CommonButton button, int tab)> _tabButtons = new List<(CommonButton, int)>();

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
            _rightTab = 0;
            _fetchedKey = string.Empty;
            RogueRun.PrepareDay(daily: true);
            RogueRunner.CaptureModEnvironment(_state);
            Build();
            RefreshOperators();
            RefreshSelections();
            RefreshRightPanel();
            LadderClient.FlushPending();
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
            _tabButtons.Clear();
            _logText = null;
            _ladderText = null;
            _rightHeader = null;
            _ladderToggle = null;
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
            _lengthButton = AddRowButton(leftPanel, "ui.dive.length", CycleLength);

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

            Transform tabRow = AddTabRow(rightPanel);
            _tabButtons.Add((AddRowButton(tabRow, "ui.dive.tablog", () => SelectTab(0)), 0));
            _tabButtons.Add((AddRowButton(tabRow, "ui.dive.tabladder", () => SelectTab(1)), 1));

            _rightHeader = AddSectionHeader(rightPanel, "ui.dive.logheader", "DIVE LOG");
            _logText = AddPanelText(rightPanel, _rightHeader.fontSize);
            _ladderText = AddPanelText(rightPanel, _rightHeader.fontSize);
            _ladderToggle = AddRowButton(rightPanel, "ui.dive.laddertoggle", ToggleLadder);

            _root.AddComponent<DiveTicker>();
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

        private static Transform AddTabRow(Transform parent)
        {
            GameObject row = new GameObject("DiveTabs", typeof(RectTransform));
            row.transform.SetParent(parent, worldPositionStays: false);
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            LayoutElement element = row.AddComponent<LayoutElement>();
            element.preferredHeight = Mathf.Max(_uiFontSize * 2.2f, 30f);
            return row.transform;
        }

        private static TextMeshProUGUI AddPanelText(Transform parent, float headerFontSize)
        {
            GameObject holder = CloneLabel(parent);
            TextMeshProUGUI text = holder.GetComponent<TextMeshProUGUI>();
            LayoutElement element = holder.AddComponent<LayoutElement>();
            element.flexibleHeight = 1f;
            text.enableAutoSizing = false;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Truncate;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.fontSize = headerFontSize * 0.9f;
            text.lineSpacing = 8f;
            UnityEngine.Object.Destroy(holder.GetComponent<LocalizableLabel>());
            return text;
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
            RefreshRightPanel();
        }

        private static void SelectTier(RogueTier tier)
        {
            _tier = tier;
            RefreshSelections();
            RefreshRightPanel();
        }

        private static void SelectTab(int tab)
        {
            _rightTab = tab;
            RefreshRightPanel();
        }

        private static void CycleLength()
        {
            _lengthIndex = (_lengthIndex + 1) % RogueConfig.FloorChoices.Length;
            RefreshSelections();
        }

        private static void ToggleLadder()
        {
            LadderConfig.SetEnabled(!LadderConfig.Enabled);
            if (LadderConfig.Enabled)
            {
                LadderClient.FlushPending();
            }
            RefreshRightPanel();
        }

        public static void NotifyLadderChanged()
        {
            if (_root == null || _rightTab != 1)
            {
                return;
            }
            RefreshLadderText();
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
            if (_lengthButton != null)
            {
                _lengthButton.gameObject.SetActive(!_daily);
                int floors = RogueConfig.FloorChoices[_lengthIndex];
                LocalizationInjector.Set("ui.dive.length", $"LENGTH: {floors} FLOORS - {RogueConfig.FloorChoiceLabels[_lengthIndex]}");
                _lengthButton.ChangeLabel("ui.dive.length");
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

        private static void RefreshRightPanel()
        {
            bool ladder = _rightTab == 1;

            LocalizationInjector.Set("ui.dive.tablog", Caption("DIVE LOG", !ladder));
            LocalizationInjector.Set("ui.dive.tabladder", Caption("LADDER", ladder));
            foreach ((CommonButton button, int tab) in _tabButtons)
            {
                button.ChangeLabel(tab == 0 ? "ui.dive.tablog" : "ui.dive.tabladder");
            }

            LocalizationInjector.Set("ui.dive.logheader", ladder ? "DAILY LADDER" : "DIVE LOG");
            if (_rightHeader != null)
            {
                LocalizableLabel headerLabel = _rightHeader.gameObject.GetComponent<LocalizableLabel>();
                if (headerLabel != null)
                {
                    headerLabel.ChangeLabel("ui.dive.logheader");
                }
            }

            if (_logText != null)
            {
                _logText.gameObject.SetActive(!ladder);
            }
            if (_ladderText != null)
            {
                _ladderText.gameObject.SetActive(ladder);
            }
            if (_ladderToggle != null)
            {
                _ladderToggle.gameObject.SetActive(ladder);
                LocalizationInjector.Set("ui.dive.laddertoggle",
                    LadderConfig.Enabled ? "SUBMIT MY DIVES: ON" : "SUBMIT MY DIVES: OFF");
                _ladderToggle.ChangeLabel("ui.dive.laddertoggle");
            }

            if (ladder)
            {
                EnsureLadderFetched();
                RefreshLadderText();
            }
            else
            {
                RefreshLog();
            }
        }

        private static string LadderDay()
        {
            return _daily ? RogueRun.DayLabel : LadderClient.TodayUtc();
        }

        private static void EnsureLadderFetched()
        {
            if (!LadderConfig.Configured)
            {
                return;
            }
            string key = LadderDay();
            if (_fetchedKey == key && LadderClient.BoardStatus != LadderStatus.Idle)
            {
                return;
            }
            _fetchedKey = key;
            LadderClient.Fetch(key);
        }

        private static void RefreshLadderText()
        {
            if (_ladderText == null)
            {
                return;
            }

            string day = LadderDay();
            string text = day + "\n\n";

            if (!LadderConfig.Configured)
            {
                text += "No ladder server is configured in this build.\n\nPoint the mod at one from the console:\nrogue_ladder endpoint <url>";
                _ladderText.text = text;
                return;
            }

            LadderBoard board = LadderClient.Board;
            bool boardMatches = board != null && board.Day == day;

            if (LadderClient.BoardStatus == LadderStatus.Loading)
            {
                text += "Contacting the ladder...";
            }
            else if (LadderClient.BoardStatus == LadderStatus.Failed)
            {
                text += "Ladder unreachable.\n" + LadderClient.BoardError;
            }
            else if (boardMatches && board.Entries.Count > 0)
            {
                bool anyModded = false;
                for (int i = 0; i < board.Entries.Count; i++)
                {
                    LadderEntry entry = board.Entries[i];
                    anyModded |= entry.Modded;
                    string tierTag = string.IsNullOrEmpty(entry.Tier) ? string.Empty : (", " + entry.Tier.ToUpperInvariant());
                    text += entry.Rank + ". " + entry.Score + " pts - floor " + entry.Floor
                        + ", " + entry.Kills + " kills" + tierTag + " - " + entry.Name
                        + (entry.Modded ? "*" : string.Empty)
                        + (entry.Victory ? " - VICTORY" : string.Empty) + "\n";
                }
                text += "\n" + board.Total + (board.Total == 1 ? " diver ranked today." : " divers ranked today.");
                if (anyModded)
                {
                    text += "\n* other mods were active";
                }
            }
            else if (boardMatches)
            {
                text += "Nobody has posted a score today yet.\nBe the first.";
            }
            else
            {
                text += "Ladder idle.";
            }

            text += "\n\n";
            if (LadderConfig.Enabled)
            {
                text += "Submission is ON. Finished daily dives send your Steam name, Steam id and run stats.";
                if (!SteamIdentity.Available)
                {
                    text += "\nSteam is not running, so submissions are paused.";
                }
            }
            else
            {
                text += "Submission is OFF. Nothing about you is sent. Turning it on sends your Steam name, Steam id and run stats when a daily dive ends - reading this board sends neither.";
            }

            if (!_daily)
            {
                text += "\n\nRandom dives are never ranked.";
            }
            if (RogueRun.ActiveMods.Count > 0)
            {
                text += "\n\nOther mods are active - your dives will carry a * on the board.";
            }
            if (!string.IsNullOrEmpty(LadderClient.LastSubmitResult))
            {
                text += "\n\nLast dive: " + LadderClient.LastSubmitResult;
            }

            _ladderText.text = text;
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
            List<RogueScoreEntry> todays = new List<RogueScoreEntry>();
            string today = RogueRun.TodayLabel();
            foreach (RogueScoreEntry entry in store.Entries)
            {
                if (entry.Daily && entry.Day == today)
                {
                    todays.Add(entry);
                }
            }
            if (todays.Count > 0)
            {
                text += "TODAY'S DAILY\n";
                for (int i = 0; i < todays.Count && i < 8; i++)
                {
                    text += FormatScoreRow(i, todays[i], showMode: false);
                }
                text += "\n";
            }
            if (store.Entries.Count > 0)
            {
                text += "TOP DIVES\n";
                int count = Mathf.Min(8, store.Entries.Count);
                for (int i = 0; i < count; i++)
                {
                    text += FormatScoreRow(i, store.Entries[i], showMode: true);
                }
            }
            if (string.IsNullOrEmpty(text))
            {
                text = "No dives recorded yet.\n\nPick a mode, difficulty and operator, then START DIVE.\n\nReach the bottom floor, seize the GOLDEN KEYCARD, evacuate alive.";
            }
            _logText.text = text;
        }

        private static string FormatScoreRow(int index, RogueScoreEntry entry, bool showMode)
        {
            string floors = entry.TotalFloors > 0 ? $"{entry.Floor}/{entry.TotalFloors}" : entry.Floor.ToString();
            string name = Localization.Get("spec." + entry.ProfileId + ".name");
            string mode = showMode ? ((entry.Daily ? entry.Day : "random") + ", ") : string.Empty;
            return $"{index + 1}. {entry.Score} pts - floor {floors}, {entry.Kills} kills, {(RogueTier)entry.Tier}, {mode}{name}{(entry.Victory ? " - VICTORY" : "")}\n";
        }

        private static void StartDive()
        {
            if (RogueRun.Active)
            {
                return;
            }
            int candidate = _candidate;
            RogueTier tier = _tier;
            int floors = _daily ? RogueConfig.DefaultFloorCount : RogueConfig.FloorChoices[_lengthIndex];
            Close();
            RogueRunner.Get(_state).BeginRun(candidate, tier, floors);
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
