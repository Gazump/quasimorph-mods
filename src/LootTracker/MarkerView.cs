using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LootTracker
{
    internal sealed class MarkerView : MonoBehaviour
    {
        private GameObject _root;
        private TextMeshProUGUI _text;
        private Graphic[] _tinted;
        private bool _broken;

        public void Show(string glyph, Color color)
        {
            if (_broken)
            {
                return;
            }
            if (_root == null && !Build())
            {
                return;
            }

            if (_text != null && _text.text != glyph)
            {
                _text.text = glyph;
            }

            for (int i = 0; i < _tinted.Length; i++)
            {
                _tinted[i].color = color;
            }

            if (!_root.activeSelf)
            {
                _root.SetActive(true);
            }
        }

        public void Hide()
        {
            if (_root != null && _root.activeSelf)
            {
                _root.SetActive(false);
            }
        }

        private bool Build()
        {
            ModConfig config = ModMain.Config;

            _root = new GameObject("LootTrackerMarker", typeof(RectTransform));
            _root.transform.SetParent(transform, false);
            var rootRect = (RectTransform)_root.transform;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            var tinted = new List<Graphic>(5);

            if (config.ShowBox)
            {
                float t = Mathf.Max(0.5f, config.BoxThickness);
                tinted.Add(Edge("Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, t)));
                tinted.Add(Edge("Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, t)));
                tinted.Add(Edge("Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(t, 0f)));
                tinted.Add(Edge("Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(t, 0f)));
            }

            if (config.ShowGlyph)
            {
                TextMeshProUGUI template = GetComponentInChildren<TextMeshProUGUI>(true);
                if (template == null)
                {
                    Log.Warn("No TextMeshPro found on an item slot; drawing the outline only.");
                }
                else
                {
                    var host = new GameObject("Glyph", typeof(RectTransform));
                    host.transform.SetParent(_root.transform, false);

                    _text = host.AddComponent<TextMeshProUGUI>();
                    _text.font = template.font;
                    _text.fontSharedMaterial = template.fontSharedMaterial;
                    _text.fontSize = config.FontSize;
                    _text.alignment = TextAlignmentOptions.Center;
                    _text.enableWordWrapping = false;
                    _text.overflowMode = TextOverflowModes.Overflow;
                    _text.raycastTarget = false;

                    var rect = (RectTransform)host.transform;
                    rect.anchorMin = config.Anchor;
                    rect.anchorMax = config.Anchor;
                    rect.pivot = config.Anchor;
                    rect.sizeDelta = new Vector2(12f, 12f);
                    rect.anchoredPosition = new Vector2(
                        config.Anchor.x > 0.5f ? config.OffsetX : -config.OffsetX,
                        config.Anchor.y > 0.5f ? config.OffsetY : -config.OffsetY);

                    tinted.Add(_text);
                }
            }

            if (tinted.Count == 0)
            {
                _broken = true;
                Object.Destroy(_root);
                _root = null;
                return false;
            }

            _tinted = tinted.ToArray();
            _root.transform.SetAsLastSibling();
            return true;
        }

        private Image Edge(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta)
        {
            var host = new GameObject(name, typeof(RectTransform));
            host.transform.SetParent(_root.transform, false);

            var image = host.AddComponent<Image>();
            image.raycastTarget = false;

            var rect = (RectTransform)host.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = Vector2.zero;
            return image;
        }
    }
}
