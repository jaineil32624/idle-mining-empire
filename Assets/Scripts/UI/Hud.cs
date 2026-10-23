using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace IdleMiningEmpire
{
    /// <summary>Always-visible HUD: currencies, zone, level/XP, backpack, income, pets, quest tracker, sell button, menu bar, touch controls.</summary>
    public class Hud
    {
        readonly GameManager gm;
        readonly UIManager ui;
        readonly RectTransform root;

        Text coinsText, gemsText, crystalsText, zoneText, levelText, bagText, incomeText, sellText, sellSub;
        Text questTitle, questProgress;
        Image xpFill, bagFill, questFill;
        GameObject crystalPill, questClaimGo, touchRoot, stationHint;
        UIFactory.UIButton sellBtn, questClaim;
        RectTransform petRow;
        readonly List<(string id, UIFactory.UIButton btn, Text badge, GameObject badgeGo)> menuButtons = new List<(string, UIFactory.UIButton, Text, GameObject)>();
        double shownCoins;
        float textTimer;
        string petSignature = "";
        ButtonPop sellPop;

        public Hud(GameManager gm, UIManager ui, RectTransform parent)
        {
            this.gm = gm;
            this.ui = ui;
            root = UIFactory.Rect("HUD", parent);
            UIFactory.Stretch(root);
            shownCoins = gm.Currency.Coins;
            BuildCurrencies();
            BuildTopCenter();
            BuildTopRight();
            BuildQuestTracker();
            BuildSell();
            BuildMenuBar();
            BuildTouchControls();
        }

        // ---------------------------------------------------------------- Build
        RectTransform Pill(Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, out Text text, Sprite icon, Color textColor, int fontSize = 40)
        {
            var bg = UIFactory.Img(root, UIFactory.HudBg, SpriteFactory.RoundedRect, "Pill");
            UIFactory.Place(bg.rectTransform, anchor, pivot, pos, size);
            UIFactory.HLayout(bg.gameObject, 12, UIFactory.Pad(14, 18, 6, 6), TextAnchor.MiddleLeft, true, true, false, false);
            if (icon != null) UIFactory.Icon(bg.transform, icon, size.y - 18);
            text = UIFactory.Txt(bg.transform, "", fontSize, textColor, TextAnchor.MiddleLeft, true);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIFactory.LE(text.gameObject, -1, -1, 1);
            return bg.rectTransform;
        }

        void BuildCurrencies()
        {
            Pill(new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -14), new Vector2(390, 74), out coinsText, SpriteFactory.CoinIcon, UIFactory.Gold, 42);
            Pill(new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -96), new Vector2(300, 60), out gemsText, SpriteFactory.GemIcon, new Color(1f, 0.6f, 0.9f), 34);
            crystalPill = Pill(new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -164), new Vector2(300, 60), out crystalsText, SpriteFactory.CrystalIcon, new Color(0.5f, 1f, 1f), 34).gameObject;
        }

        void BuildTopCenter()
        {
            var banner = UIFactory.Img(root, UIFactory.HudBg, SpriteFactory.RoundedRect, "ZoneBanner");
            UIFactory.Place(banner.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -14), new Vector2(640, 64));
            zoneText = UIFactory.Txt(banner.transform, "", 34, UIFactory.Gold, TextAnchor.MiddleCenter, true);
            UIFactory.Stretch(zoneText.rectTransform, 10, 0, 10, 0);
            var zb = banner.gameObject.AddComponent<Button>();
            zb.transition = Selectable.Transition.None;
            banner.raycastTarget = true;
            zb.onClick.AddListener(() => ui.Toggle("zones"));

            var lvl = UIFactory.Img(root, UIFactory.HudBg, SpriteFactory.RoundedRect, "Level");
            UIFactory.Place(lvl.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -84), new Vector2(640, 50));
            var barBg = UIFactory.Img(lvl.transform, new Color(0, 0, 0, 0.5f), SpriteFactory.RoundedRect, "XpBg");
            UIFactory.Stretch(barBg.rectTransform, 8, 8, 8, 8);
            xpFill = UIFactory.Img(barBg.transform, new Color(0.35f, 0.85f, 0.45f), SpriteFactory.RoundedRect, "XpFill");
            UIFactory.Stretch(xpFill.rectTransform);
            xpFill.rectTransform.anchorMax = new Vector2(0, 1);
            levelText = UIFactory.Txt(lvl.transform, "", 26, Color.white, TextAnchor.MiddleCenter, true);
            UIFactory.Stretch(levelText.rectTransform);
        }

        void BuildTopRight()
        {
            var bag = Pill(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-16, -14), new Vector2(400, 74), out bagText, SpriteFactory.BagIcon, Color.white, 34);
            var barBg = UIFactory.Img(bag, new Color(0, 0, 0, 0.45f), SpriteFactory.RoundedRect, "BagBar");
            barBg.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            UIFactory.Stretch(barBg.rectTransform, 76, 52, 14, 8);
            bagFill = UIFactory.Img(barBg.transform, UIFactory.Green, SpriteFactory.RoundedRect, "Fill");
            UIFactory.Stretch(bagFill.rectTransform);
            bagFill.rectTransform.anchorMax = new Vector2(0, 1);

            Pill(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-16, -96), new Vector2(400, 54), out incomeText, SpriteFactory.CoinIcon, new Color(0.7f, 1f, 0.7f), 28);

            petRow = UIFactory.Rect("Pets", root);
            UIFactory.Place(petRow, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-16, -160), new Vector2(400, 64));
            UIFactory.HLayout(petRow.gameObject, 8, null, TextAnchor.MiddleRight, false, false, false, false);
        }

        void BuildQuestTracker()
        {
            var card = UIFactory.Img(root, UIFactory.HudBg, SpriteFactory.RoundedRect, "QuestTracker");
            UIFactory.Place(card.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(16, 80), new Vector2(470, 170));
            card.raycastTarget = true;
            var cb = card.gameObject.AddComponent<Button>();
            cb.transition = Selectable.Transition.None;
            cb.onClick.AddListener(() => ui.Toggle("quests"));

            var head = UIFactory.Txt(card.transform, "QUEST", 22, UIFactory.TextDim, TextAnchor.UpperLeft, true);
            UIFactory.Place(head.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -10), new Vector2(300, 30));
            questTitle = UIFactory.Txt(card.transform, "", 28, Color.white, TextAnchor.UpperLeft, true);
            UIFactory.Place(questTitle.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -40), new Vector2(434, 70));

            var barBg = UIFactory.Img(card.transform, new Color(0, 0, 0, 0.5f), SpriteFactory.RoundedRect, "Bar");
            UIFactory.Place(barBg.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(18, 16), new Vector2(434, 34));
            questFill = UIFactory.Img(barBg.transform, UIFactory.Gold, SpriteFactory.RoundedRect, "Fill");
            UIFactory.Stretch(questFill.rectTransform);
            questFill.rectTransform.anchorMax = new Vector2(0, 1);
            questProgress = UIFactory.Txt(barBg.transform, "", 22, Color.white, TextAnchor.MiddleCenter, true);
            UIFactory.Stretch(questProgress.rectTransform);

            questClaim = UIFactory.Btn(card.transform, "CLAIM!", UIFactory.Green, () =>
            {
                var q = gm.Quests.Story ?? gm.Quests.Active.FirstOrDefault(x => gm.Quests.IsComplete(x) && !x.claimed);
                if (q != null) gm.Quests.Claim(q);
            }, 30);
            UIFactory.Place(questClaim.rect, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-12, -10), new Vector2(150, 60));
            questClaim.button.gameObject.AddComponent<Spin>().speed = 0;
            questClaimGo = questClaim.button.gameObject;
        }

        void BuildSell()
        {
            sellBtn = UIFactory.Btn(root, "", new Color(0.95f, 0.65f, 0.1f), () => ui.SellPressed(), 40);
            UIFactory.Place(sellBtn.rect, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-16, 60), new Vector2(300, 150));
            sellText = UIFactory.Txt(sellBtn.button.transform, "SELL", 44, Color.white, TextAnchor.UpperCenter, true);
            UIFactory.Stretch(sellText.rectTransform, 6, 14, 6, 50);
            sellSub = UIFactory.Txt(sellBtn.button.transform, "", 24, new Color(1f, 1f, 0.85f), TextAnchor.LowerCenter, true);
            UIFactory.Stretch(sellSub.rectTransform, 6, 80, 6, 14);
            sellPop = sellBtn.button.GetComponent<ButtonPop>();

            var hint = UIFactory.Img(root, new Color(0.1f, 0.35f, 0.15f, 0.9f), SpriteFactory.RoundedRect, "StationHint");
            UIFactory.Place(hint.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-16, 160), new Vector2(300, 44));
            var ht = UIFactory.Txt(hint.transform, "At Trading Post: 100%", 22, Color.white, TextAnchor.MiddleCenter, true);
            UIFactory.Stretch(ht.rectTransform);
            stationHint = hint.gameObject;
        }

        void BuildMenuBar()
        {
            var bar = UIFactory.Img(root, UIFactory.HudBg, SpriteFactory.White, "MenuBar");
            var rt = bar.rectTransform;
            rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.offsetMin = new Vector2(0, 0); rt.offsetMax = new Vector2(0, 118);
            bar.raycastTarget = true;
            UIFactory.HLayout(bar.gameObject, 8, UIFactory.Pad(12, 12, 10, 12), TextAnchor.MiddleCenter, true, true, true, true);

            (string id, string label, Color c)[] items =
            {
                ("upgrades", "Upgrades", UIFactory.Blue),
                ("shop", "Shop", new Color(0.2f, 0.65f, 0.55f)),
                ("inventory", "Backpack", new Color(0.55f, 0.4f, 0.25f)),
                ("research", "Research", UIFactory.Purple),
                ("pets", "Pets", new Color(0.85f, 0.4f, 0.65f)),
                ("quests", "Quests", UIFactory.Orange),
                ("zones", "Elevator", new Color(0.35f, 0.45f, 0.6f)),
                ("prestige", "Rebirth", new Color(0.15f, 0.6f, 0.75f)),
                ("more", "More", new Color(0.35f, 0.35f, 0.45f)),
            };
            for (int i = 0; i < items.Length; i++)
            {
                var it = items[i];
                var b = UIFactory.Btn(bar.transform, it.label, it.c, () => ui.Toggle(it.id), 28);
                var key = UIFactory.Txt(b.button.transform, (i + 1).ToString(), 16, new Color(1, 1, 1, 0.5f), TextAnchor.UpperLeft, true);
                UIFactory.Stretch(key.rectTransform, 10, 4, 0, 0);

                var badge = UIFactory.Img(b.button.transform, UIFactory.Red, SpriteFactory.Circle, "Badge");
                UIFactory.Place(badge.rectTransform, new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-10, -8), new Vector2(38, 38));
                var bt = UIFactory.Txt(badge.transform, "!", 22, Color.white, TextAnchor.MiddleCenter, true);
                UIFactory.Stretch(bt.rectTransform);
                badge.gameObject.SetActive(false);
                menuButtons.Add((it.id, b, bt, badge.gameObject));
            }
        }

        void BuildTouchControls()
        {
            touchRoot = UIFactory.Rect("TouchControls", root).gameObject;
            UIFactory.Stretch((RectTransform)touchRoot.transform);

            HoldButton Make(string name, Sprite icon, float rot, Vector2 anchor, Vector2 pos, float size)
            {
                var bg = UIFactory.Img(touchRoot.transform, new Color(1, 1, 1, 0.16f), SpriteFactory.Circle, name);
                bg.raycastTarget = true;
                UIFactory.Place(bg.rectTransform, anchor, new Vector2(0.5f, 0.5f), pos, new Vector2(size, size));
                var ic = UIFactory.Img(bg.transform, new Color(1, 1, 1, 0.85f), icon, "Icon");
                ic.type = Image.Type.Simple;
                UIFactory.Place(ic.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 0.45f, size * 0.45f));
                ic.rectTransform.localRotation = Quaternion.Euler(0, 0, rot);
                return bg.gameObject.AddComponent<HoldButton>();
            }

            var left = Make("Left", SpriteFactory.Arrow, 180, new Vector2(0, 0), new Vector2(110, 230), 170);
            var right = Make("Right", SpriteFactory.Arrow, 0, new Vector2(0, 0), new Vector2(300, 230), 170);
            var jump = Make("Jump", SpriteFactory.UpArrow, 0, new Vector2(1, 0), new Vector2(-110, 230), 170);
            var dash = Make("Dash", SpriteFactory.Arrow, 0, new Vector2(1, 0), new Vector2(-300, 230), 140);
            left.onDown = () => Input2D.VirtualAxis = -1;
            left.onUp = () => { if (Input2D.VirtualAxis < 0) Input2D.VirtualAxis = 0; };
            right.onDown = () => Input2D.VirtualAxis = 1;
            right.onUp = () => { if (Input2D.VirtualAxis > 0) Input2D.VirtualAxis = 0; };
            jump.onDown = Input2D.PressVirtualJump;
            dash.onDown = Input2D.PressVirtualDash;
        }

        // ---------------------------------------------------------------- Update
        public void PulseSell() { if (sellPop != null) sellPop.Pulse(); }

        public void Update(float dt)
        {
            // animated coin counter
            double coins = gm.Currency.Coins;
            shownCoins = System.Math.Abs(coins - shownCoins) < 0.5 ? coins : shownCoins + (coins - shownCoins) * Mathf.Min(1f, dt * 10f);

            float fill = Mathf.Clamp01((float)(gm.Inventory.TotalCount / Mathf.Max(1, gm.Inventory.Capacity)));
            bagFill.rectTransform.anchorMax = new Vector2(fill, 1);
            bagFill.color = Color.Lerp(UIFactory.Green, UIFactory.Red, fill > 0.8f ? (fill - 0.8f) * 5f : 0);
            xpFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01((float)gm.Levels.Progress), 1);

            bool touch = ui.TouchControlsVisible && !ui.IsModalOpen;
            if (touchRoot.activeSelf != touch) touchRoot.SetActive(touch);
            if (!touch && Input2D.VirtualAxis != 0) Input2D.VirtualAxis = 0;

            textTimer -= dt;
            coinsText.text = NumberFormatter.Format(shownCoins);
            if (textTimer > 0) return;
            textTimer = 0.12f;

            gemsText.text = NumberFormatter.Format(gm.Currency.Gems);
            bool showCrystals = gm.Currency.Crystals > 0 || gm.Save.stats.prestiges > 0;
            if (crystalPill.activeSelf != showCrystals) crystalPill.SetActive(showCrystals);
            crystalsText.text = NumberFormatter.Format(gm.Currency.Crystals);

            zoneText.text = gm.Zones.DisplayName(gm.Zones.Current);
            levelText.text = "Lv " + gm.Levels.Level + "  " + gm.Levels.Title +
                             (gm.Levels.Level < LevelManager.MaxLevel ? "   (" + NumberFormatter.Format(gm.Levels.Xp) + " / " + NumberFormatter.Format(LevelManager.XpToNext(gm.Levels.Level)) + " XP)" : "  MAX");

            bagText.text = NumberFormatter.Format(gm.Inventory.TotalCount) + " / " + NumberFormatter.Format(gm.Inventory.Capacity);
            double cps = gm.Automation.CoinsPerSecond;
            incomeText.text = cps > 0 ? "+" + NumberFormatter.Format(cps) + "/s idle" : "No idle income yet";

            double rate = ui.SellRate;
            double value = gm.Inventory.TotalValue * rate;
            sellText.text = "SELL";
            sellSub.text = "+" + NumberFormatter.Format(value) + "  (" + Mathf.RoundToInt((float)rate * 100) + "%)";
            stationHint.SetActive(gm.Zones.AtStation);

            UpdateQuestTracker();
            UpdatePets();
            UpdateBadges();
        }

        void UpdateQuestTracker()
        {
            var q = gm.Quests.Story ?? (gm.Quests.Active.Count > 0 ? gm.Quests.Active[0] : null);
            var claimable = gm.Quests.Active.FirstOrDefault(x => !x.claimed && gm.Quests.IsComplete(x));
            if (claimable != null) q = claimable;
            if (q == null)
            {
                questTitle.text = "All quests complete!";
                questProgress.text = "";
                questFill.rectTransform.anchorMax = new Vector2(1, 1);
                questClaimGo.SetActive(false);
                return;
            }
            questTitle.text = q.title;
            float f = Mathf.Clamp01((float)(q.progress / System.Math.Max(1e-9, q.amount)));
            questFill.rectTransform.anchorMax = new Vector2(f, 1);
            questProgress.text = NumberFormatter.Format(q.progress) + " / " + NumberFormatter.Format(q.amount);
            questClaimGo.SetActive(gm.Quests.IsComplete(q));
        }

        void UpdatePets()
        {
            var eq = gm.Pets.Equipped.ToList();
            string sig = string.Join(",", eq.Select(p => p.uid));
            if (sig == petSignature) return;
            petSignature = sig;
            for (int i = petRow.childCount - 1; i >= 0; i--) Object.Destroy(petRow.GetChild(i).gameObject);
            foreach (var p in eq)
            {
                var d = gm.Pets.Def(p);
                if (d == null) continue;
                var bg = UIFactory.Img(petRow, UIFactory.HudBg, SpriteFactory.RoundedRect, "Pet");
                bg.rectTransform.sizeDelta = new Vector2(64, 64);
                var frame = UIFactory.Img(bg.transform, PetManager.RarityColor(d.rarity), SpriteFactory.RoundedFrame, "Frame");
                UIFactory.Stretch(frame.rectTransform);
                var ic = UIFactory.Img(bg.transform, Color.white, SpriteFactory.Pet(d), "Icon");
                ic.type = Image.Type.Simple;
                ic.preserveAspect = true;
                UIFactory.Stretch(ic.rectTransform, 8, 8, 8, 8);
            }
        }

        void UpdateBadges()
        {
            foreach (var (id, btn, badge, badgeGo) in menuButtons)
            {
                string txt = null;
                bool locked = false;
                switch (id)
                {
                    case "quests":
                        int c = gm.Quests.ClaimableCount;
                        if (c > 0) txt = c.ToString();
                        break;
                    case "upgrades":
                        if (gm.Db.upgrades.Any(u => u.category <= UpgradeCategory.Movement && gm.Upgrades.CanBuy(u) && u.id == "pick_power")) txt = "!";
                        break;
                    case "zones":
                        int next = gm.Save.unlockedZone + 1;
                        if (next < gm.Db.zones.Count && gm.Zones.UnlockBlocker(next) == null) txt = "!";
                        break;
                    case "prestige":
                        if (gm.Prestige.CanPrestige) txt = "+" + NumberFormatter.Format(gm.Prestige.CrystalsOnPrestige);
                        break;
                    case "pets":
                        locked = !gm.Pets.FeatureUnlocked;
                        break;
                    case "research":
                        locked = !gm.Research.FeatureUnlocked;
                        if (!locked && gm.Db.research.Any(n => gm.Research.CanResearch(n))) txt = "!";
                        break;
                }
                badgeGo.SetActive(txt != null);
                if (txt != null) badge.text = txt;
                btn.bg.color = locked ? Color.Lerp(btn.color, UIFactory.Disabled, 0.7f) : btn.color;
            }
        }
    }
}
