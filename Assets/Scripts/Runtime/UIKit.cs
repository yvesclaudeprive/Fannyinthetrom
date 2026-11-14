using System;
using UnityEngine;
using UnityEngine.UI;

namespace FannyIn2Trom
{
    /// <summary>Raccourcis pour construire l'interface (uGUI) entièrement par code.</summary>
    public static class UIKit
    {
        static Font font;
        public static Font Font
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5; // UI
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        /// <summary>Place l'élément selon des ancres normalisées (0..1) de son parent.</summary>
        public static RectTransform Anchor(RectTransform rt, float xMin, float yMin, float xMax, float yMax)
        {
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image Image(string name, Transform parent, Sprite sprite, Vector2 size, Vector2 pos, Color? color = null)
        {
            var rt = Rect(name, parent);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color ?? Color.white;
            img.raycastTarget = false;
            return img;
        }

        public static Image Panel(string name, Transform parent, Color color)
        {
            var img = Image(name, parent, null, Vector2.zero, Vector2.zero, color);
            Stretch(img.rectTransform);
            return img;
        }

        public static Text Text(string name, Transform parent, string text, int size, Color color, Vector2 boxSize, Vector2 pos,
            TextAnchor align = TextAnchor.MiddleCenter, bool outline = true)
        {
            var rt = Rect(name, parent);
            rt.sizeDelta = boxSize;
            rt.anchoredPosition = pos;
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = FontStyle.Bold;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            if (outline)
            {
                var o = rt.gameObject.AddComponent<Outline>();
                o.effectColor = new Color(0.05f, 0.05f, 0.12f, 0.9f);
                o.effectDistance = new Vector2(3f, -3f);
            }
            return t;
        }

        public static Button Button(string name, Transform parent, string label, Vector2 size, Vector2 pos, Color color, Action onClick)
        {
            var img = Image(name, parent, Art.RoundedRect, size, pos, color);
            img.type = UnityEngine.UI.Image.Type.Sliced;
            img.raycastTarget = true;
            var b = img.gameObject.AddComponent<Button>();
            var colors = b.colors;
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            b.colors = colors;
            b.onClick.AddListener(() =>
            {
                Sfx.Play(Sfx.Click);
                if (onClick != null) onClick();
            });
            var t = Text("Label", img.transform, label, Mathf.RoundToInt(size.y * 0.42f), Color.white, size, Vector2.zero);
            Stretch(t.rectTransform);
            return b;
        }

        /// <summary>Taille d'un sprite pour une hauteur donnée en respectant ses proportions.</summary>
        public static Vector2 SizeForHeight(Sprite s, float h)
        {
            if (s == null) return new Vector2(h, h);
            return new Vector2(h * s.rect.width / s.rect.height, h);
        }
    }
}
