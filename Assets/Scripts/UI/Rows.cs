using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace IdleMiningEmpire
{
    /// <summary>A standard list row: icon, title, subtitle and action buttons.</summary>
    public class Row
    {
        public RectTransform rt;
        public Image bg, icon;
        public Text title, sub;
        public RectTransform buttons;
    }

    public static class Rows
    {
        public static Row Make(RectTransform parent, Sprite icon, string title, string sub, float height = 124, Color? bg = null, Color? iconTint = null)
        {
            var r = new Row();
            r.bg = UIFactory.Img(parent, bg ?? UIFactory.PanelMid, SpriteFactory.RoundedRect, "Row");
            r.rt = r.bg.rectTransform;
            UIFactory.LE(r.bg.gameObject, -1, height, 1, 0, -1, height);
            UIFactory.HLayout(r.bg.gameObject, 18, UIFactory.Pad(18, 16, 10, 10), TextAnchor.MiddleLeft, true, true, false, false);

            if (icon != null)
            {
                var holder = UIFactory.Img(r.bg.transform, new Color(0, 0, 0, 0.25f), SpriteFactory.RoundedRect, "IconBg");
                UIFactory.LE(holder.gameObject, height - 22, height - 22, 0, 0, height - 22, height - 22);
                r.icon = UIFactory.Img(holder.transform, iconTint ?? Color.white, icon, "Icon");
                r.icon.type = Image.Type.Simple;
                r.icon.preserveAspect = true;
                UIFactory.Stretch(r.icon.rectTransform, 10, 10, 10, 10);
            }

            var col = UIFactory.Rect("Text", r.bg.transform);
            UIFactory.VLayout(col.gameObject, 2, null, TextAnchor.MiddleLeft, true, true, true, false);
            UIFactory.LE(col.gameObject, 200, -1, 1, 0);
            r.title = UIFactory.Txt(col, title, 30, UIFactory.TextMain, TextAnchor.MiddleLeft, true);
            r.sub = UIFactory.Txt(col, sub, 22, UIFactory.TextDim, TextAnchor.UpperLeft);
            if (string.IsNullOrEmpty(sub)) r.sub.gameObject.SetActive(false);

            r.buttons = UIFactory.Rect("Buttons", r.bg.transform);
            UIFactory.HLayout(r.buttons.gameObject, 10, null, TextAnchor.MiddleRight, true, true, false, false);
            return r;
        }

        public static UIFactory.UIButton Button(Row r, string label, Color c, UnityAction a, float width = 230, float height = 90)
        {
            var b = UIFactory.Btn(r.buttons, label, c, a, 26);
            UIFactory.LE(b.button.gameObject, width, height, 0, 0, width, height);
            return b;
        }

        public static Text Header(RectTransform parent, string text, Color? color = null, int size = 30)
        {
            var t = UIFactory.Txt(parent, text, size, color ?? UIFactory.Gold, TextAnchor.MiddleLeft, true);
            UIFactory.LE(t.gameObject, -1, size + 22);
            return t;
        }

        public static Text Paragraph(RectTransform parent, string text, int size = 26, Color? color = null)
        {
            var t = UIFactory.Txt(parent, text, size, color ?? UIFactory.TextMain, TextAnchor.UpperLeft);
            UIFactory.LE(t.gameObject, -1, -1);
            return t;
        }

        public static string CostLabel(CurrencyType c, double cost)
        {
            string sym = c == CurrencyType.Coins ? "" : c == CurrencyType.Gems ? " gems" : " crystals";
            return NumberFormatter.Format(cost) + sym;
        }
    }
}
