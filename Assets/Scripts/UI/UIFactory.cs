using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace IdleMiningEmpire
{
    /// <summary>Theme + helpers for building a polished uGUI interface entirely from code.</summary>
    public static class UIFactory
    {
        // ---- Theme ------------------------------------------------------------
        public static readonly Color PanelBg = new Color(0.08f, 0.09f, 0.16f, 0.97f);
        public static readonly Color PanelMid = new Color(0.13f, 0.15f, 0.25f, 1f);
        public static readonly Color PanelLight = new Color(0.19f, 0.21f, 0.34f, 1f);
        public static readonly Color HudBg = new Color(0.05f, 0.06f, 0.12f, 0.78f);
        public static readonly Color TextMain = new Color(0.96f, 0.96f, 1f);
        public static readonly Color TextDim = new Color(0.68f, 0.71f, 0.82f);
        public static readonly Color Gold = new Color(1f, 0.8f, 0.2f);
        public static readonly Color Green = new Color(0.27f, 0.78f, 0.42f);
        public static readonly Color Blue = new Color(0.27f, 0.52f, 0.95f);
        public static readonly Color Purple = new Color(0.58f, 0.36f, 0.95f);
        public static readonly Color Red = new Color(0.9f, 0.3f, 0.3f);
        public static readonly Color Orange = new Color(0.97f, 0.55f, 0.18f);
        public static readonly Color Disabled = new Color(0.3f, 0.32f, 0.4f);

        static Font font;
        public static Font DefaultFont
        {
            get
            {
                if (font != null) return font;
                try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch (Exception) { }
                if (font == null) { try { font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch (Exception) { } }
                if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 32);
                return font;
            }
        }

        // ---- Rect helpers ----------------------------------------------------
        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5; // UI
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt, float left = 0, float top = 0, float right = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        // ---- Widgets -----------------------------------------------------------
        public static Image Img(Transform parent, Color color, Sprite sprite = null, string name = "Image")
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite != null ? sprite : SpriteFactory.RoundedRect;
            img.type = img.sprite == SpriteFactory.RoundedRect || img.sprite == SpriteFactory.RoundedFrame ? Image.Type.Sliced : Image.Type.Simple;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public static Image Icon(Transform parent, Sprite sprite, float size, Color? tint = null)
        {
            var img = Img(parent, tint ?? Color.white, sprite, "Icon");
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            LE(img.gameObject, size, size, 0, 0, size, size);
            img.rectTransform.sizeDelta = new Vector2(size, size);
            return img;
        }

        public static Text Txt(Transform parent, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleLeft, bool bold = false)
        {
            var rt = Rect("Text", parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = DefaultFont;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.supportRichText = true;
            var sh = rt.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0, 0, 0, 0.55f);
            sh.effectDistance = new Vector2(1.5f, -1.5f);
            return t;
        }

        public class UIButton
        {
            public Button button;
            public Image bg;
            public Text label;
            public Color color;
            public RectTransform rect => (RectTransform)button.transform;

            public void Set(string text, bool interactable, Color? col = null)
            {
                if (label != null && label.text != text) label.text = text;
                button.interactable = interactable;
                var c = col ?? color;
                bg.color = interactable ? c : Color.Lerp(c, Disabled, 0.7f);
                if (label != null) label.color = interactable ? Color.white : new Color(1, 1, 1, 0.55f);
            }
        }

        public static UIButton Btn(Transform parent, string label, Color color, UnityAction onClick, int fontSize = 26)
        {
            var img = Img(parent, color, SpriteFactory.RoundedRect, "Button");
            img.raycastTarget = true;
            var b = img.gameObject.AddComponent<Button>();
            var cb = b.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.1f, 1.1f, 1.1f);
            cb.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            cb.disabledColor = Color.white;
            cb.fadeDuration = 0.06f;
            b.colors = cb;
            b.targetGraphic = img;
            if (onClick != null) b.onClick.AddListener(onClick);
            b.onClick.AddListener(() => { if (GameManager.Instance != null) GameManager.Instance.Audio.Play(Sfx.Click, 0.35f, 0.02f); });
            img.gameObject.AddComponent<ButtonPop>();

            // subtle top highlight
            var hl = Img(img.transform, new Color(1, 1, 1, 0.12f), SpriteFactory.RoundedRect, "Highlight");
            Stretch(hl.rectTransform, 3, 3, 3, 0);
            hl.rectTransform.anchorMin = new Vector2(0, 0.5f);

            Text t = null;
            if (!string.IsNullOrEmpty(label))
            {
                t = Txt(img.transform, label, fontSize, Color.white, TextAnchor.MiddleCenter, true);
                Stretch(t.rectTransform, 6, 2, 6, 2);
                t.horizontalOverflow = HorizontalWrapMode.Wrap;
                t.resizeTextForBestFit = true;
                t.resizeTextMinSize = 12;
                t.resizeTextMaxSize = fontSize;
            }
            return new UIButton { button = b, bg = img, label = t, color = color };
        }

        // ---- Layout ------------------------------------------------------------
        public static LayoutElement LE(GameObject go, float prefW = -1, float prefH = -1, float flexW = -1, float flexH = -1, float minW = -1, float minH = -1)
        {
            var le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            le.preferredWidth = prefW; le.preferredHeight = prefH;
            le.flexibleWidth = flexW; le.flexibleHeight = flexH;
            le.minWidth = minW; le.minHeight = minH;
            return le;
        }

        public static HorizontalLayoutGroup HLayout(GameObject go, float spacing, RectOffset pad = null, TextAnchor align = TextAnchor.MiddleLeft,
            bool controlW = true, bool controlH = true, bool expandW = false, bool expandH = false)
        {
            var l = go.AddComponent<HorizontalLayoutGroup>();
            l.spacing = spacing;
            l.padding = pad ?? new RectOffset(0, 0, 0, 0);
            l.childAlignment = align;
            l.childControlWidth = controlW; l.childControlHeight = controlH;
            l.childForceExpandWidth = expandW; l.childForceExpandHeight = expandH;
            return l;
        }

        public static VerticalLayoutGroup VLayout(GameObject go, float spacing, RectOffset pad = null, TextAnchor align = TextAnchor.UpperLeft,
            bool controlW = true, bool controlH = true, bool expandW = true, bool expandH = false)
        {
            var l = go.AddComponent<VerticalLayoutGroup>();
            l.spacing = spacing;
            l.padding = pad ?? new RectOffset(0, 0, 0, 0);
            l.childAlignment = align;
            l.childControlWidth = controlW; l.childControlHeight = controlH;
            l.childForceExpandWidth = expandW; l.childForceExpandHeight = expandH;
            return l;
        }

        /// <summary>Vertical scroll view; returns the content rect (auto-sized by its layout).</summary>
        public static RectTransform Scroll(Transform parent, out ScrollRect scroll)
        {
            var root = Rect("Scroll", parent);
            scroll = root.gameObject.AddComponent<ScrollRect>();
            var bgImg = root.gameObject.AddComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.001f); // catch drags

            var viewport = Rect("Viewport", root);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = Rect("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 40f;
            scroll.inertia = true;
            return content;
        }

        public static RectOffset Pad(int all) => new RectOffset(all, all, all, all);
        public static RectOffset Pad(int l, int r, int t, int b) => new RectOffset(l, r, t, b);

        public static string Hex(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);
        public static string Colorize(string s, Color c) => "<color=" + Hex(c) + ">" + s + "</color>";
    }
}
