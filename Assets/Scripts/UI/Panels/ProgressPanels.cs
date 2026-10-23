using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace IdleMiningEmpire
{
    /// <summary>The research tree: six category columns, nine tiers each, with prerequisite chains.</summary>
    public class ResearchPanel : UIPanel
    {
        public override string Title => "Research Tree";

        public override void Build(RectTransform content, int tab)
        {
            if (!gm.Research.FeatureUnlocked)
            {
                Rows.Paragraph(content, "The Research Tree unlocks at player level " + ResearchManager.UnlockLevel + ". Keep mining!", 30, UIFactory.TextDim);
                return;
            }
            var info = Rows.Paragraph(content, "", 24, UIFactory.TextDim);
            ui.AddRefresher(() => info.text = "Completed " + gm.Research.CompletedCount + " / " + gm.Db.research.Count +
                                              ".  Research is permanent and survives Rebirth. Tap a node for details.");

            var grid = UIFactory.Rect("Grid", content);
            UIFactory.HLayout(grid.gameObject, 10, null, TextAnchor.UpperCenter, true, true, true, false);
            var cats = (ResearchCategory[])Enum.GetValues(typeof(ResearchCategory));
            foreach (var cat in cats)
            {
                var col = UIFactory.Rect(cat.ToString(), grid);
                UIFactory.VLayout(col.gameObject, 0, null, TextAnchor.UpperCenter, true, true, true, false);
                UIFactory.LE(col.gameObject, 150, -1, 1, 0);
                var head = UIFactory.Img(col, ResearchManager.CategoryColor(cat) * 0.7f + new Color(0, 0, 0, 0.3f), SpriteFactory.RoundedRect, "Head");
                UIFactory.LE(head.gameObject, -1, 64);
                var ht = UIFactory.Txt(head.transform, ResearchManager.CategoryName(cat), 24, Color.white, TextAnchor.MiddleCenter, true);
                UIFactory.Stretch(ht.rectTransform, 4, 0, 4, 0);

                var nodes = gm.Db.research.Where(n => n.category == cat).OrderBy(n => n.tier).ToList();
                foreach (var n in nodes)
                {
                    // connector line
                    var link = UIFactory.Rect("Link", col);
                    UIFactory.LE(link.gameObject, -1, 18);
                    var line = UIFactory.Img(link, new Color(1, 1, 1, 0.2f), SpriteFactory.White, "Line");
                    UIFactory.Place(line.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(6, 18));

                    var node = n;
                    var btn = UIFactory.Btn(col, "", UIFactory.PanelLight, () => ShowNode(node), 22);
                    UIFactory.LE(btn.button.gameObject, -1, 112);
                    var name = UIFactory.Txt(btn.button.transform, node.displayName, 21, Color.white, TextAnchor.UpperCenter, true);
                    UIFactory.Stretch(name.rectTransform, 6, 8, 6, 40);
                    var cost = UIFactory.Txt(btn.button.transform, "", 19, UIFactory.Gold, TextAnchor.LowerCenter, true);
                    UIFactory.Stretch(cost.rectTransform, 4, 60, 4, 8);
                    var catColor = ResearchManager.CategoryColor(cat);
                    ui.AddRefresher(() =>
                    {
                        bool done = gm.Research.IsDone(node);
                        bool avail = !done && gm.Research.PrereqMet(node);
                        bool afford = gm.Research.CanResearch(node);
                        btn.button.interactable = true;
                        btn.bg.color = done ? new Color(0.2f, 0.55f, 0.3f) : avail ? (afford ? catColor * 0.85f + new Color(0, 0, 0, 0.15f) : catColor * 0.45f + new Color(0, 0, 0, 0.55f)) : UIFactory.Disabled;
                        line.color = done ? new Color(0.4f, 1f, 0.5f, 0.8f) : new Color(1, 1, 1, 0.2f);
                        cost.text = done ? "DONE" : avail ? CostText(node) : "LOCKED";
                        cost.color = done ? Color.white : afford ? UIFactory.Gold : new Color(1, 1, 1, 0.6f);
                    });
                }
            }
        }

        static string CostText(ResearchNodeDefinition n)
        {
            string s = NumberFormatter.Format(n.costCoins);
            if (n.costGems > 0) s += " + " + NumberFormatter.Format(n.costGems) + "g";
            return s;
        }

        void ShowNode(ResearchNodeDefinition n)
        {
            string effects = string.Join("\n", n.modifiers.Select(m => m.Describe(1)));
            string state = gm.Research.IsDone(n) ? UIFactory.Colorize("Completed", UIFactory.Green)
                : !gm.Research.PrereqMet(n) ? UIFactory.Colorize("Requires: " + (gm.Db.GetResearch(n.prerequisiteId)?.displayName ?? "previous node"), UIFactory.Red)
                : "Cost: " + UIFactory.Colorize(NumberFormatter.Format(n.costCoins) + " coins" + (n.costGems > 0 ? " + " + NumberFormatter.Format(n.costGems) + " gems" : ""), UIFactory.Gold);
            string body = n.description + "\n\n" + UIFactory.Colorize(effects, new Color(0.6f, 1f, 0.7f)) + "\n\n" +
                          ResearchManager.CategoryName(n.category) + " - Tier " + (n.tier + 1) + "\n" + state;
            if (gm.Research.CanResearch(n))
                ui.ShowPopup(n.displayName, body, "Research", () => { gm.Research.TryResearch(n); ui.Rebuild(); }, "Close");
            else
                ui.ShowPopup(n.displayName, body, "OK", null);
        }
    }

    /// <summary>Hatch eggs and manage the pet collection.</summary>
    public class PetsPanel : UIPanel
    {
        public override string Title => "Pets";
        public override string[] Tabs => new[] { "Hatch Eggs", "Collection" };

        public override void Build(RectTransform content, int tab)
        {
            if (!gm.Pets.FeatureUnlocked)
            {
                Rows.Paragraph(content, "Pets unlock at player level " + PetManager.UnlockLevel + ". They boost your stats and level up automatically as you mine!", 30, UIFactory.TextDim);
                return;
            }
            if (tab == 0) BuildHatch(content);
            else BuildCollection(content);
        }

        void BuildHatch(RectTransform content)
        {
            Rows.Paragraph(content, "Pets give Mining Speed, Luck, Ore Value, Backpack Size or Coin bonuses. Equipped pets gain XP from every rock you break and level up automatically (max " + PetManager.MaxPetLevel + ").", 24, UIFactory.TextDim);
            for (int i = 0; i < gm.Pets.Eggs.Length; i++)
            {
                int idx = i;
                var egg = gm.Pets.Eggs[i];
                string odds = string.Join("  ", Enumerable.Range(0, 5).Where(r => egg.odds[r] > 0)
                    .Select(r => UIFactory.Colorize(((Rarity)r).ToString() + " " + egg.odds[r] + "%", PetManager.RarityColor((Rarity)r))));
                var row = Rows.Make(content, SpriteFactory.Egg(egg.color), egg.name, odds, 130);
                var btn = Rows.Button(row, "", CurrencyManager.ColorOf(egg.currency) * 0.8f + new Color(0, 0, 0, 0.2f), () => Hatch(idx), 260);
                ui.AddRefresher(() =>
                {
                    double cost = gm.Pets.EggCost(idx);
                    btn.Set("Hatch\n" + Rows.CostLabel(egg.currency, cost), gm.Currency.CanAfford(egg.currency, cost),
                        CurrencyManager.ColorOf(egg.currency) * 0.8f + new Color(0, 0, 0, 0.2f));
                });
            }
        }

        void Hatch(int idx)
        {
            var inst = gm.Pets.Hatch(idx);
            if (inst == null) return;
            var d = gm.Pets.Def(inst);
            gm.Fx.Confetti();
            ui.ShowPopup("You hatched " + d.displayName + "!",
                UIFactory.Colorize(d.rarity.ToString().ToUpperInvariant(), PetManager.RarityColor(d.rarity)) + "\n\n+" +
                NumberFormatter.Percent(gm.Pets.Bonus(inst)) + " " + StatInfo.Name(d.bonusStat) +
                (inst.equipped ? "\n\nEquipped automatically." : "\n\nAll slots full - equip it from your Collection."),
                "Awesome!", () => ui.Rebuild(), null, null, SpriteFactory.Pet(d), PetManager.RarityColor(d.rarity) * 0.8f + new Color(0, 0, 0, 0.2f));
        }

        void BuildCollection(RectTransform content)
        {
            var head = Rows.Make(content, SpriteFactory.StarIcon, "Equipped", "", 110);
            Rows.Button(head, "Equip Best", UIFactory.Green, () => { gm.Pets.EquipBest(); ui.Rebuild(); }, 240);
            ui.AddRefresher(() =>
            {
                head.title.text = "Equipped " + gm.Pets.EquippedCount + " / " + gm.Pets.Slots + "   " +
                                  UIFactory.Colorize("Pets " + gm.Pets.All.Count + "/" + PetManager.MaxPets, UIFactory.TextDim);
            });

            if (gm.Pets.All.Count == 0) Rows.Paragraph(content, "No pets yet - hatch an egg!", 28, UIFactory.TextDim);
            var sorted = gm.Pets.All.OrderByDescending(p => p.equipped)
                .ThenByDescending(p => (int)(gm.Pets.Def(p)?.rarity ?? Rarity.Common))
                .ThenByDescending(p => p.level).ToList();
            foreach (var p in sorted)
            {
                var d = gm.Pets.Def(p);
                if (d == null) continue;
                var pet = p;
                var row = Rows.Make(content, SpriteFactory.Pet(d), d.displayName, "", 118,
                    pet.equipped ? new Color(0.16f, 0.28f, 0.22f) : (Color?)null);
                var eq = Rows.Button(row, "", UIFactory.Blue, () => { gm.Pets.SetEquipped(pet, !pet.equipped); ui.Rebuild(); }, 180);
                Rows.Button(row, "Release\n+" + NumberFormatter.Format(gm.Pets.ReleaseValue(pet)) + " gems", UIFactory.Red * 0.8f + new Color(0, 0, 0, 0.2f), () =>
                {
                    ui.ShowConfirm("Release " + d.displayName + "?", "You'll get " + NumberFormatter.Format(gm.Pets.ReleaseValue(pet)) + " gems. This can't be undone.",
                        "Release", () => { gm.Pets.Release(pet); ui.Rebuild(); }, UIFactory.Red);
                }, 180);
                ui.AddRefresher(() =>
                {
                    row.title.text = UIFactory.Colorize(d.displayName, PetManager.RarityColor(d.rarity)) + "  Lv " + pet.level;
                    row.sub.gameObject.SetActive(true);
                    row.sub.text = d.rarity + "  |  +" + NumberFormatter.Percent(gm.Pets.Bonus(pet)) + " " + StatInfo.Name(d.bonusStat) +
                                   (pet.level < PetManager.MaxPetLevel ? "  |  XP " + NumberFormatter.Format(pet.xp) + "/" + NumberFormatter.Format(PetManager.XpToNext(pet.level)) : "  |  MAX LEVEL");
                    eq.Set(pet.equipped ? "Unequip" : "Equip", true, pet.equipped ? UIFactory.PanelLight : UIFactory.Blue);
                });
            }
        }
    }

    /// <summary>Story quest + rotating side quests.</summary>
    public class QuestsPanel : UIPanel
    {
        public override string Title => "Quests";

        public override void Build(RectTransform content, int tab)
        {
            Rows.Paragraph(content, "Story quests guide you to the Earth's Core and beyond. Side quests refresh as you complete them and scale with your progress.", 24, UIFactory.TextDim);
            foreach (var q in gm.Quests.Active.ToList())
            {
                var quest = q;
                var row = Rows.Make(content, quest.isStory ? SpriteFactory.StarIcon : SpriteFactory.CoinIcon, quest.title, "", 124,
                    quest.isStory ? new Color(0.22f, 0.2f, 0.12f) : (Color?)null);
                var claim = Rows.Button(row, "Claim", UIFactory.Green, () => { gm.Quests.Claim(quest); ui.Rebuild(); }, 200);
                ui.AddRefresher(() =>
                {
                    bool done = gm.Quests.IsComplete(quest);
                    row.title.text = (quest.isStory ? UIFactory.Colorize("STORY  ", UIFactory.Gold) : "") + quest.title;
                    row.sub.gameObject.SetActive(true);
                    row.sub.text = "Progress: " + NumberFormatter.Format(quest.progress) + " / " + NumberFormatter.Format(quest.amount) +
                                   "\nReward: " + QuestManager.RewardText(quest);
                    claim.Set(done ? "Claim!" : Mathf.FloorToInt((float)(quest.progress / Math.Max(1e-9, quest.amount) * 100)) + "%", done);
                });
            }
            if (gm.Quests.Story == null && gm.Save.storyIndex >= gm.Db.storyQuests.Count)
                Rows.Paragraph(content, UIFactory.Colorize("Story complete! The Abyss goes on forever - how deep can you go?", UIFactory.Gold), 26);
        }
    }
}
