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
        private static CommonButton _ladderToggle;
        private static string _fetchedKey = string.Empty;

        private static bool _daily = true;
        private static RogueTier _tier = RogueTier.Normal;
        private static int _candidate;
        private static int _lengthIndex = 1;
        private static CommonButton _lengthButton;
        private static CommonButton _tierButton;

        private static readonly List<(CommonButton button, bool daily)> _modeButtons = new List<(CommonButton, bool)>();
        private static readonly List<CommonButton> _operatorButtons = new List<CommonButton>();
        private static readonly List<Image> _classIcons = new List<Image>();
        private static readonly List<ClassTooltipHover> _classHovers = new List<ClassTooltipHover>();

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
            _operatorButtons.Clear();
            _classIcons.Clear();
            _classHovers.Clear();
            _logText = null;
            _ladderText = null;
            _ladderToggle = null;
            _lengthButton = null;
            _tierButton = null;
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
            _tierButton = AddRowButton(leftPanel, "ui.dive.difficulty", CycleTier);

            AddSectionHeader(leftPanel, "ui.dive.operator", "OPERATOR");
            _operatorSection = leftPanel;
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                _operatorButtons.Add(AddOperatorRow(leftPanel, "ui.dive.merc" + i, () => SelectOperator(index)));
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

            GameObject columns = new GameObject("DiveColumns", typeof(RectTransform));
            columns.transform.SetParent(rightPanel, worldPositionStays: false);
            HorizontalLayoutGroup columnsLayout = columns.AddComponent<HorizontalLayoutGroup>();
            columnsLayout.spacing = 20f;
            columnsLayout.childControlWidth = true;
            columnsLayout.childControlHeight = true;
            columnsLayout.childForceExpandWidth = true;
            columnsLayout.childForceExpandHeight = true;
            LayoutElement columnsElement = columns.AddComponent<LayoutElement>();
            columnsElement.flexibleHeight = 1f;

            Transform logColumn = AddColumn(columns.transform);
            Transform ladderColumn = AddColumn(columns.transform);

            TextMeshProUGUI logHeader = AddSectionHeader(logColumn, "ui.dive.logheader", "DIVE LOG");
            _logText = AddScrollableText(logColumn, logHeader.fontSize);
            TextMeshProUGUI ladderHeader = AddSectionHeader(ladderColumn, "ui.dive.ladderheader", "DAILY LADDER");
            _ladderText = AddScrollableText(ladderColumn, ladderHeader.fontSize);
            _ladderToggle = AddRowButton(ladderColumn, "ui.dive.laddertoggle", ToggleLadder);

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

        private static Transform AddColumn(Transform parent)
        {
            GameObject column = new GameObject("DiveColumn", typeof(RectTransform));
            column.transform.SetParent(parent, worldPositionStays: false);
            VerticalLayoutGroup layout = column.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return column.transform;
        }

        private static TextMeshProUGUI AddScrollableText(Transform parent, float headerFontSize)
        {
            GameObject viewport = new GameObject("DiveScroll", typeof(RectTransform));
            viewport.transform.SetParent(parent, worldPositionStays: false);
            LayoutElement element = viewport.AddComponent<LayoutElement>();
            element.flexibleHeight = 1f;
            viewport.AddComponent<RectMask2D>();
            ScrollRect scroll = viewport.AddComponent<ScrollRect>();
            GameObject holder = CloneLabel(viewport.transform);
            TextMeshProUGUI text = holder.GetComponent<TextMeshProUGUI>();
            text.enableAutoSizing = false;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Overflow;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.fontSize = headerFontSize * 0.9f;
            text.lineSpacing = 8f;
            UnityEngine.Object.Destroy(holder.GetComponent<LocalizableLabel>());
            RectTransform content = holder.transform as RectTransform;
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = new Vector2(0f, content.offsetMin.y);
            content.offsetMax = new Vector2(0f, content.offsetMax.y);
            content.anchoredPosition = Vector2.zero;
            ContentSizeFitter fitter = holder.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;
            scroll.viewport = viewport.transform as RectTransform;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;
            return text;
        }

        private static CommonButton AddOperatorRow(Transform parent, string captionKey, Action onClick)
        {
            float height = Mathf.Max(_uiFontSize * 2.2f, 30f);
            GameObject row = new GameObject("DiveOperatorRow", typeof(RectTransform));
            row.transform.SetParent(parent, worldPositionStays: false);
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            LayoutElement rowElement = row.AddComponent<LayoutElement>();
            rowElement.preferredHeight = height;
            GameObject iconObject = new GameObject("DiveClassIcon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(row.transform, worldPositionStays: false);
            LayoutElement iconElement = iconObject.AddComponent<LayoutElement>();
            iconElement.preferredWidth = height;
            iconElement.preferredHeight = height;
            Image image = iconObject.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = true;
            image.enabled = false;
            _classIcons.Add(image);
            _classHovers.Add(iconObject.AddComponent<ClassTooltipHover>());
            CommonButton button = AddRowButton(row.transform, captionKey, onClick);
            LayoutElement buttonElement = button.gameObject.GetComponent<LayoutElement>();
            buttonElement.flexibleWidth = 1f;
            return button;
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

        private static void CycleTier()
        {
            _tier = (RogueTier)(((int)_tier + 1) % 3);
            RefreshSelections();
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
            if (_root == null)
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
                    _operatorButtons[i].transform.parent.gameObject.SetActive(false);
                    continue;
                }
                Mercenary mercenary = candidates[i];
                _operatorButtons[i].transform.parent.gameObject.SetActive(true);
                MercTooltipHover hover = _operatorButtons[i].gameObject.GetComponent<MercTooltipHover>();
                if (hover == null)
                {
                    hover = _operatorButtons[i].gameObject.AddComponent<MercTooltipHover>();
                }
                hover.Merc = mercenary;
                hover.Profile = Data.MercenaryProfiles.GetRecord(mercenary.ProfileId);
                if (i < _classIcons.Count)
                {
                    MercenaryClassDescriptor classDescriptor = Data.MercenaryClasses.GetRecord(mercenary.MercClassId)?.ContentDescriptor as MercenaryClassDescriptor;
                    Sprite classSprite = classDescriptor != null ? (classDescriptor.SmallIcon != null ? classDescriptor.SmallIcon : classDescriptor.Icon) : null;
                    _classIcons[i].sprite = classSprite;
                    _classIcons[i].enabled = classSprite != null;
                    _classHovers[i].ClassId = mercenary.MercClassId;
                }
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
                LocalizationInjector.Set("ui.dive.length", $"LENGTH:  < {floors} FLOORS - {RogueConfig.FloorChoiceLabels[_lengthIndex]} >");
                _lengthButton.ChangeLabel("ui.dive.length");
            }
            if (_tierButton != null)
            {
                LocalizationInjector.Set("ui.dive.difficulty", $"DIFFICULTY:  < {_tier.ToString().ToUpperInvariant()} >");
                _tierButton.ChangeLabel("ui.dive.difficulty");
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
            if (_ladderToggle != null)
            {
                LocalizationInjector.Set("ui.dive.laddertoggle",
                    LadderConfig.Enabled ? "SUBMIT MY DIVES: ON" : "SUBMIT MY DIVES: OFF");
                _ladderToggle.ChangeLabel("ui.dive.laddertoggle");
            }
            RefreshLog();
            EnsureLadderFetched();
            RefreshLadderText();
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
