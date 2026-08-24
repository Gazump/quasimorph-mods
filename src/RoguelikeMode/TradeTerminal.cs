using System;
using System.Collections.Generic;
using HarmonyLib;
using MGSC;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace RoguelikeMode
{
    public static class TradeTerminal
    {
        private static State _state;
        private static MapObstacle _obstacle;
        private static Store _store;
        private static string _obstacleId;
        private static Sprite[] _onFrames;
        private static Sprite[] _offFrames;

        public static ItemStorage Storage => _store != null ? _store.storage : null;

        public static void Spawn(State state)
        {
            _state = state;
            _obstacle = null;
            _store = null;
            RogueRun.TerminalUsed = false;
            try
            {
                SpawnInternal(state);
            }
            catch (Exception ex)
            {
                Debug.LogError("[RoguelikeMode] Barter terminal spawn failed: " + ex);
            }
        }

        private static void SpawnInternal(State state)
        {
            MapObstacleFactory factory = SingletonMonoBehaviour<MapObstacleFactory>.Instance;
            Creatures creatures = state.Get<Creatures>();
            MapGrid mapGrid = state.Get<MapGrid>();
            ItemsOnFloor itemsOnFloor = state.Get<ItemsOnFloor>();
            MapObstacles mapObstacles = state.Get<MapObstacles>();
            Player player = creatures?.Player;
            if (factory == null || player == null || mapGrid == null || mapObstacles == null)
            {
                return;
            }
            string obstacleId = FindTerminalObstacleId(factory);
            if (obstacleId == null)
            {
                Debug.LogWarning("[RoguelikeMode] No container obstacle available for the barter terminal.");
                return;
            }
            List<CellPosition> positions = new List<CellPosition>();
            MapSystem.GetValidSpawnPositionsInRadius(positions, mapGrid, creatures, itemsOnFloor, mapObstacles, player.CreatureData.Position, 2, 8, MapSystem.ValidCellRule.Capsule);
            if (positions.Count == 0)
            {
                MapSystem.GetValidSpawnPositionsInRadius(positions, mapGrid, creatures, itemsOnFloor, mapObstacles, player.CreatureData.Position, 2, 16, MapSystem.ValidCellRule.Capsule);
            }
            if (positions.Count == 0)
            {
                Debug.LogWarning("[RoguelikeMode] No valid cell for the barter terminal on this floor.");
                return;
            }
            System.Random rng = new System.Random(RogueRun.SeedFor("terminal:" + RogueRun.CurrentLocationId));
            CellPosition pos = positions[rng.Next(positions.Count)];
            MapObstacle obstacle = factory.Spawn(obstacleId, pos, CellPosition.Zero, register: true, refreshOccupation: true);
            if (obstacle == null)
            {
                return;
            }
            Store store = obstacle.GetComponent<Store>();
            if (store == null)
            {
                factory.KillObstacle(obstacle);
                return;
            }
            _obstacle = obstacle;
            _store = store;
            ApplyTerminalLook();
            RogueRun.TerminalPosition = pos.X + "," + pos.Y;
            Debug.Log($"[RoguelikeMode] Barter terminal ({obstacleId}) placed at {pos}.");
        }

        public static void Rebind(State state)
        {
            _state = state;
            _obstacle = null;
            _store = null;
            try
            {
                string[] parts = (RogueRun.TerminalPosition ?? string.Empty).Split(',');
                if (parts.Length != 2 || !int.TryParse(parts[0], out int x) || !int.TryParse(parts[1], out int y))
                {
                    return;
                }
                MapObstacles mapObstacles = state.Get<MapObstacles>();
                if (mapObstacles == null)
                {
                    return;
                }
                foreach (MapObstacle obstacle in mapObstacles.Obstacles)
                {
                    if (obstacle == null || obstacle.Position.X != x || obstacle.Position.Y != y)
                    {
                        continue;
                    }
                    Store store = obstacle.GetComponent<Store>();
                    if (store == null)
                    {
                        continue;
                    }
                    _obstacle = obstacle;
                    _store = store;
                    ApplyTerminalLook();
                    Debug.Log($"[RoguelikeMode] Barter terminal rebound at ({x},{y}).");
                    return;
                }
                Debug.LogWarning($"[RoguelikeMode] Barter terminal not found at ({x},{y}) after resume.");
            }
            catch (Exception ex)
            {
                Debug.LogError("[RoguelikeMode] Barter terminal rebind failed: " + ex);
            }
        }

        private static void ApplyTerminalLook()
        {
            AccessTools.Field(typeof(Store), "_captionTag").SetValue(_store, RogueConfig.TerminalCaptionKey);
            Type accessMode = AccessTools.Inner(typeof(Store), "AccessMode");
            AccessTools.Field(typeof(Store), "_accessMode").SetValue(_store, Enum.ToObject(accessMode, 0));
            AccessTools.Field(typeof(Store), "_killObstacleWhenEmpty").SetValue(_store, false);
            LoadFrames();
            ApplyStateVisual();
        }

        private static void LoadFrames()
        {
            if (_onFrames != null && _offFrames != null)
            {
                return;
            }
            Sprite reference = _obstacle.Renderer != null && _obstacle.Renderer.sprite != null
                ? _obstacle.Renderer.sprite
                : _store.GetActualSprite(empty: true);
            _onFrames = Registration.LoadContentSheet(reference, "rogue_barter_terminal.png");
            _offFrames = Registration.LoadContentSheet(reference, "rogue_barter_terminal_off.png");
        }

        private static void ApplyStateVisual()
        {
            Sprite[] frames = RogueRun.TerminalUsed ? _offFrames : _onFrames;
            if (frames == null || frames.Length == 0)
            {
                _store.RefreshVisual();
                return;
            }
            Sprite[] still = { frames[0] };
            AccessTools.Field(typeof(Store), "_filledSprites").SetValue(_store, still);
            AccessTools.Field(typeof(Store), "_emptySprites").SetValue(_store, still);
            _store.RefreshVisual();
            GameObject rendererObject = _obstacle.Renderer != null ? _obstacle.Renderer.gameObject : _obstacle.gameObject;
            RogueFrameAnimator animator = rendererObject.GetComponent<RogueFrameAnimator>();
            if (animator == null)
            {
                animator = rendererObject.AddComponent<RogueFrameAnimator>();
            }
            animator.Configure(_obstacle.Renderer, frames, RogueRun.TerminalUsed ? 1f : 3f);
            ApplyLight();
        }

        private static void ApplyLight()
        {
            try
            {
                Light2D light = _obstacle.gameObject.GetComponent<Light2D>();
                if (light == null)
                {
                    light = _obstacle.gameObject.AddComponent<Light2D>();
                    int[] layers = new int[SortingLayer.layers.Length];
                    for (int i = 0; i < layers.Length; i++)
                    {
                        layers[i] = SortingLayer.layers[i].id;
                    }
                    AccessTools.Field(typeof(Light2D), "m_ApplyToSortingLayers")?.SetValue(light, layers);
                }
                light.lightType = Light2D.LightType.Point;
                light.pointLightInnerRadius = 0.4f;
                light.pointLightOuterRadius = 2.2f;
                light.shadowIntensity = 0.7f;
                light.intensity = RogueRun.TerminalUsed ? 0.35f : 1.1f;
                light.color = RogueRun.TerminalUsed ? new Color(0.6f, 0.15f, 0.12f) : new Color(0.35f, 1f, 0.55f);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RoguelikeMode] Barter terminal light unavailable: " + ex.Message);
            }
        }

        private static string FindTerminalObstacleId(MapObstacleFactory factory)
        {
            if (_obstacleId != null)
            {
                return _obstacleId;
            }
            DescriptorsCollection descriptors = AccessTools.Field(typeof(MapObstacleFactory), "_obstaclesDescriptors").GetValue(factory) as DescriptorsCollection;
            if (descriptors == null)
            {
                return null;
            }
            Dictionary<string, string> containerToId = new Dictionary<string, string>();
            string fallback = null;
            foreach (string id in descriptors.Ids)
            {
                if (!descriptors.TryGetDescriptor(id, out UnityEngine.Object descriptor))
                {
                    continue;
                }
                MapObstacle prefab = descriptor as MapObstacle;
                if (prefab == null)
                {
                    continue;
                }
                Store store = prefab.gameObject.GetComponent<Store>();
                if (store == null || string.IsNullOrEmpty(store.ContainerId))
                {
                    continue;
                }
                if (!containerToId.ContainsKey(store.ContainerId))
                {
                    containerToId.Add(store.ContainerId, id);
                }
                if (fallback == null)
                {
                    fallback = id;
                }
            }
            foreach (string containerId in RogueConfig.TerminalContainerIds)
            {
                if (containerToId.TryGetValue(containerId, out string id))
                {
                    _obstacleId = id;
                    return _obstacleId;
                }
            }
            _obstacleId = fallback;
            return _obstacleId;
        }

        public static void ExecuteTrade(ItemsStorageView view)
        {
            try
            {
                ExecuteTradeInternal(view);
            }
            catch (Exception ex)
            {
                Debug.LogError("[RoguelikeMode] Barter terminal trade failed: " + ex);
            }
        }

        private static void ExecuteTradeInternal(ItemsStorageView view)
        {
            if (!RogueRun.Active || RogueRun.TerminalUsed || _state == null || _obstacle == null || Storage == null)
            {
                return;
            }
            ItemsPrices prices = _state.Get<ItemsPrices>();
            if (prices == null)
            {
                return;
            }
            List<BasePickupItem> eligible = new List<BasePickupItem>();
            float total = 0f;
            foreach (BasePickupItem item in Storage.Items)
            {
                if (IsTradeableItem(item))
                {
                    eligible.Add(item);
                    total += prices.GetPrice(item.Id) * item.StackCount;
                }
            }
            if (eligible.Count == 0)
            {
                SingletonMonoBehaviour<TooltipFactory>.Instance.ShowSimpleTextTooltip("Nothing tradeable in the terminal.");
                return;
            }
            foreach (BasePickupItem item in eligible)
            {
                Storage.Remove(item);
            }
            int pool = Mathf.RoundToInt(total * RogueConfig.TradeInRate) + RogueRun.TradeCredit;
            List<string> candidates = BuildCandidates();
            List<string> tradeTags = new List<string>();
            foreach (BasePickupItem item in eligible)
            {
                tradeTags.Add(item.Id + ":" + item.StackCount);
            }
            tradeTags.Sort(StringComparer.Ordinal);
            int tradeSeed = RogueRun.SeedFor("trade:" + RogueRun.CurrentLocationId + ":" + pool + ":" + string.Join("|", tradeTags));
            UnityEngine.Random.State previousState = UnityEngine.Random.state;
            UnityEngine.Random.InitState(tradeSeed);
            List<GeneratedItemData> outputs = candidates.Count > 0
                ? ItemDropSystem.GenerateItems(prices, candidates, pool)
                : new List<GeneratedItemData>();
            UnityEngine.Random.state = previousState;
            int spent = 0;
            ItemFactory itemFactory = SingletonMonoBehaviour<ItemFactory>.Instance;
            foreach (GeneratedItemData output in outputs)
            {
                Storage.AddItemAndReshuffle(itemFactory.CreateForInventory(output.ItemId));
                spent += Mathf.RoundToInt(output.Points);
            }
            RogueRun.TradeCredit = Mathf.Max(0, pool - spent);
            RogueRun.TerminalUsed = true;
            if (_state != null)
            {
                RunPersistence.SaveFloorEntry(_state);
            }
            ApplyStateVisual();
            if (view != null)
            {
                AccessTools.Method(typeof(ItemsStorageView), "InitContent", new[] { typeof(ItemStorage) })?.Invoke(view, new object[] { Storage });
            }
            string credit = RogueRun.TradeCredit > 0 ? $", ${RogueRun.TradeCredit} credit carried to the next floor" : string.Empty;
            string feedback = $"Terminal exchanged {eligible.Count} item(s) for {outputs.Count}{credit}";
            SingletonMonoBehaviour<TooltipFactory>.Instance.ShowSimpleTextTooltip(feedback);
            Debug.Log("[RoguelikeMode] " + feedback);
        }

        internal static bool IsTradeableItem(BasePickupItem item)
        {
            if (item == null || item.Locked || item.IsImplicit || item.Id == RogueConfig.KeycardId)
            {
                return false;
            }
            if (!(Data.Items.GetRecord(item.Id) is CompositeItemRecord composite))
            {
                return false;
            }
            foreach (BasePickupItemRecord record in composite.Records)
            {
                if (record is TrashRecord trash && trash.SubType == TrashSubtype.QuestItem)
                {
                    return false;
                }
            }
            return true;
        }

        public static void GetTradeSummary(out int itemCount, out int payout)
        {
            itemCount = 0;
            payout = RogueRun.TradeCredit;
            if (_state == null || Storage == null)
            {
                return;
            }
            ItemsPrices prices = _state.Get<ItemsPrices>();
            if (prices == null)
            {
                return;
            }
            float total = 0f;
            foreach (BasePickupItem item in Storage.Items)
            {
                if (IsTradeableItem(item))
                {
                    itemCount++;
                    total += prices.GetPrice(item.Id) * item.StackCount;
                }
            }
            payout = Mathf.RoundToInt(total * RogueConfig.TradeInRate) + RogueRun.TradeCredit;
        }

        private static List<string> BuildCandidates()
        {
            int techLevel = RogueConfig.TechLevelForFloor(Mathf.Max(1, RogueRun.CurrentFloor));
            bool blockCases = RogueRun.Tier != RogueTier.Easy;
            List<string> ids = new List<string>();
            foreach (BasePickupItemRecord baseRecord in Data.Items.Records)
            {
                if (!(baseRecord is CompositeItemRecord composite) || !(composite.PrimaryRecord is ItemRecord primary))
                {
                    continue;
                }
                string id = primary.Id;
                if (string.IsNullOrEmpty(id) || id.Contains("_custom") || id == RogueConfig.KeycardId)
                {
                    continue;
                }
                if (primary.TechLevel > techLevel || primary.Price < 5f)
                {
                    continue;
                }
                if (primary.Categories == null || primary.Categories.Count == 0)
                {
                    continue;
                }
                if (blockCases && Array.IndexOf(RogueConfig.BlockedContainerIds, id) >= 0)
                {
                    continue;
                }
                if (!(primary.ContentDescriptor is ItemContentDescriptor descriptor) || descriptor.Icon == null)
                {
                    continue;
                }
                string nameKey = "item." + id + ".name";
                if (Localization.Get(nameKey, warnIfMissingTag: false) == nameKey)
                {
                    continue;
                }
                bool quest = false;
                foreach (BasePickupItemRecord record in composite.Records)
                {
                    if (record is TrashRecord trash && trash.SubType == TrashSubtype.QuestItem)
                    {
                        quest = true;
                        break;
                    }
                }
                if (!quest)
                {
                    ids.Add(id);
                }
            }
            return ids;
        }
    }

    public class RogueFrameAnimator : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private Sprite[] _frames;
        private float _frameDuration = 1f;
        private int _frame;
        private float _elapsed;

        public void Configure(SpriteRenderer renderer, Sprite[] frames, float framesPerSecond)
        {
            _renderer = renderer;
            _frames = frames;
            _frameDuration = framesPerSecond > 0f ? 1f / framesPerSecond : 1f;
            _frame = 0;
            _elapsed = 0f;
        }

        private void Update()
        {
            if (_renderer == null || _frames == null || _frames.Length < 2)
            {
                return;
            }
            _elapsed += Time.deltaTime;
            if (_elapsed < _frameDuration)
            {
                return;
            }
            _elapsed -= _frameDuration;
            _frame = (_frame + 1) % _frames.Length;
            if (_renderer.enabled)
            {
                _renderer.sprite = _frames[_frame];
            }
        }
    }

    public class TradeTerminalWidget : MonoBehaviour
    {
        public ItemsStorageView View;
        public CommonButton TradeButton;
        public TextMeshProUGUI ValueLabel;

        private float _nextRefresh;

        private void Update()
        {
            if (Time.unscaledTime < _nextRefresh)
            {
                return;
            }
            _nextRefresh = Time.unscaledTime + 0.25f;
            bool show = RogueRun.Active
                && !RogueRun.TerminalUsed
                && View != null
                && View.IsViewActive
                && TradeTerminal.Storage != null
                && View.Storage == TradeTerminal.Storage;
            if (TradeButton != null && TradeButton.gameObject.activeSelf != show)
            {
                TradeButton.gameObject.SetActive(show);
            }
            if (ValueLabel != null && ValueLabel.gameObject.activeSelf != show)
            {
                ValueLabel.gameObject.SetActive(show);
            }
            if (!show || ValueLabel == null || TradeButton == null)
            {
                return;
            }
            TradeTerminal.GetTradeSummary(out int itemCount, out int payout);
            ValueLabel.text = payout.ToString();
        }
    }

    [HarmonyPatch(typeof(ItemsStorageView), "Refresh")]
    public static class TradeTerminalViewPatch
    {
        public static void Postfix(ItemsStorageView __instance, bool show)
        {
            try
            {
                if (!show || !RogueRun.Active || TradeTerminal.Storage == null || __instance.Storage != TradeTerminal.Storage)
                {
                    return;
                }
                TradeTerminalWidget widget = __instance.gameObject.GetComponent<TradeTerminalWidget>();
                if (widget == null)
                {
                    widget = Build(__instance);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[RoguelikeMode] Terminal widget failed: " + ex);
            }
        }

        private static TradeTerminalWidget Build(ItemsStorageView view)
        {
            CommonButton takeAll = AccessTools.Field(typeof(ItemsStorageView), "_takeAllButton").GetValue(view) as CommonButton;
            if (takeAll == null)
            {
                return null;
            }
            RectTransform takeAllRect = takeAll.transform as RectTransform;
            GameObject buttonObject = UnityEngine.Object.Instantiate(takeAll.gameObject, takeAllRect.parent);
            buttonObject.name = "TradeInButton";
            buttonObject.SetActive(false);
            CommonButton tradeButton = buttonObject.GetComponent<CommonButton>();
            if (tradeButton is HotkeyButton hotkey)
            {
                GameKeyPanel panel = AccessTools.Field(typeof(HotkeyButton), "_gameKeyPanel").GetValue(hotkey) as GameKeyPanel;
                CommonButton fresh = buttonObject.AddComponent<CommonButton>();
                CopyButtonFields(hotkey, fresh);
                UnityEngine.Object.DestroyImmediate(hotkey);
                if (panel != null)
                {
                    UnityEngine.Object.DestroyImmediate(panel.gameObject);
                }
                tradeButton = fresh;
            }
            RectTransform buttonRect = tradeButton.transform as RectTransform;
            UnityEngine.UI.LayoutElement layoutElement = buttonObject.GetComponent<UnityEngine.UI.LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = buttonObject.AddComponent<UnityEngine.UI.LayoutElement>();
            }
            layoutElement.ignoreLayout = true;
            buttonRect.anchorMin = new Vector2(1f, takeAllRect.anchorMin.y);
            buttonRect.anchorMax = new Vector2(1f, takeAllRect.anchorMax.y);
            buttonRect.pivot = new Vector2(1f, takeAllRect.pivot.y);
            buttonRect.sizeDelta = takeAllRect.sizeDelta;
            buttonRect.anchoredPosition = new Vector2(-16f, takeAllRect.anchoredPosition.y);
            buttonObject.SetActive(true);
            tradeButton.ChangeLabel(RogueConfig.TradeInCaptionKey);
            tradeButton.OnClick += (b, clicks) => TradeTerminal.ExecuteTrade(view);
            TextMeshProUGUI valueText = null;
            if (tradeButton.CaptionLabel != null && tradeButton.CaptionLabel.Text != null)
            {
                TextMeshProUGUI caption = tradeButton.CaptionLabel.Text;
                RectTransform captionRect = caption.rectTransform;
                captionRect.anchorMin = Vector2.zero;
                captionRect.anchorMax = Vector2.one;
                captionRect.offsetMin = new Vector2(12f, 0f);
                captionRect.offsetMax = new Vector2(-12f, 0f);
                caption.alignment = TextAlignmentOptions.MidlineLeft;
                GameObject labelObject = UnityEngine.Object.Instantiate(caption.gameObject, captionRect.parent);
                labelObject.name = "TradeInValue";
                LocalizableLabel cloneLabel = labelObject.GetComponent<LocalizableLabel>();
                if (cloneLabel != null)
                {
                    UnityEngine.Object.Destroy(cloneLabel);
                }
                valueText = labelObject.GetComponent<TextMeshProUGUI>();
                RectTransform labelRect = labelObject.transform as RectTransform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(12f, 0f);
                labelRect.offsetMax = new Vector2(-12f, 0f);
                valueText.alignment = TextAlignmentOptions.MidlineRight;
                valueText.enableAutoSizing = false;
                valueText.enableWordWrapping = false;
                valueText.text = string.Empty;
            }
            TradeTerminalWidget widget = view.gameObject.AddComponent<TradeTerminalWidget>();
            widget.View = view;
            widget.TradeButton = tradeButton;
            widget.ValueLabel = valueText;
            return widget;
        }

        private static void CopyButtonFields(CommonButton source, CommonButton target)
        {
            Type type = typeof(CommonButton);
            while (type != null && type.Namespace == "MGSC")
            {
                foreach (System.Reflection.FieldInfo field in type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly))
                {
                    field.SetValue(target, field.GetValue(source));
                }
                type = type.BaseType;
            }
        }
    }
}
