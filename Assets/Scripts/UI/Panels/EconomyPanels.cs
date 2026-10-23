using System.Linq;
using UnityEngine;

namespace IdleMiningEmpire
{
    /// <summary>Pickaxe / Backpack / Movement (coins) and Premium (gems) upgrades.</summary>
    public class UpgradesPanel : UIPanel
    {
        public override string Title => "Upgrades";
        public override string[] Tabs => new[] { "Pickaxe", "Backpack", "Movement", "Premium" };

        public override void Build(RectTransform content, int tab)
        {
            var cat = (UpgradeCategory)tab;
            if (cat == UpgradeCategory.Pickaxe)
            {
                var stats = Rows.Paragraph(content, "", 24, UIFactory.TextDim);
                ui.AddRefresher(() =>
                {
                    var z = gm.Zones.Current;
                    double need = gm.Zones.Hardness(z, gm.Zones.Depth(z));
                    stats.text =
                        "Mining Power " + UIFactory.Colorize(NumberFormatter.Format(gm.Stat(StatType.MiningPower)), UIFactory.Gold) +
                        "   Crit " + StatInfo.FormatFinal(StatType.CritChance, gm.Stat(StatType.CritChance)) +
                        " / " + StatInfo.FormatFinal(StatType.CritDamage, gm.Stat(StatType.CritDamage)) +
                        "   Speed " + StatInfo.FormatFinal(StatType.MiningSpeed, gm.Stat(StatType.MiningSpeed)) +
                        "   Luck " + StatInfo.FormatFinal(StatType.Luck, gm.Stat(StatType.Luck)) +
                        "\nThis zone needs " + NumberFormatter.Format(need) + " power" +
                        (gm.Stat(StatType.MiningPower) < need ? UIFactory.Colorize("  (too weak: 75% damage penalty!)", UIFactory.Red) : "");
                });
            }
            if (cat == UpgradeCategory.Premium)
                Rows.Paragraph(content, "Premium upgrades cost gems and are kept forever, even through Rebirth. Earn gems from quests, level-ups, achievements, rare rocks and guardians.", 24, UIFactory.TextDim);

            foreach (var u in gm.Upgrades.InCategory(cat).ToList()) AddUpgradeRow(gm, ui, content, u);
        }

        public static void AddUpgradeRow(GameManager gm, UIManager ui, RectTransform content, UpgradeDefinition u)
        {
            var row = Rows.Make(content, SpriteFactory.IconForStat(u.modifier.stat), u.displayName, u.description);
            var buy = Rows.Button(row, "", CurrencyManager.ColorOf(u.currency) * 0.85f + new Color(0, 0, 0, 0.15f), () => gm.Upgrades.Buy(u), 240);
            UIFactory.UIButton max = null;
            if (u.currency == CurrencyType.Coins)
                max = Rows.Button(row, "MAX", UIFactory.Green, () => gm.Upgrades.BuyMax(u), 120);
            ui.AddRefresher(() =>
            {
                int l = gm.Upgrades.Level(u);
                bool maxed = u.IsMaxed(l);
                row.title.text = u.displayName + "  " + UIFactory.Colorize("Lv " + l + (u.maxLevel > 0 ? "/" + u.maxLevel : ""), UIFactory.Gold);
                string now = l > 0 ? u.modifier.Describe(l) : "none";
                row.sub.text = u.description + "\nNow: " + now + (maxed ? "   " + UIFactory.Colorize("MAXED", UIFactory.Green) : "   Next: " + u.modifier.Describe(l + 1));
                if (maxed) buy.Set("MAXED", false, UIFactory.Disabled);
                else buy.Set("Buy\n" + Rows.CostLabel(u.currency, u.CostAt(l)), gm.Upgrades.CanBuy(u), CurrencyManager.ColorOf(u.currency) * 0.85f + new Color(0, 0, 0, 0.15f));
                if (max != null) max.Set("MAX", !maxed && gm.Upgrades.CanBuy(u));
            });
        }
    }

    /// <summary>Workers and drills.</summary>
    public class ShopPanel : UIPanel
    {
        public override string Title => "Automation Shop";
        public override string[] Tabs => new[] { "Workers", "Drills" };

        public override void Build(RectTransform content, int tab)
        {
            var summary = Rows.Paragraph(content, "", 26);
            ui.AddRefresher(() =>
            {
                summary.text = "Idle income: " + UIFactory.Colorize("+" + NumberFormatter.Format(gm.Automation.CoinsPerSecond) + " coins/s", UIFactory.Gold) +
                               "   (workers " + NumberFormatter.Format(gm.Automation.WorkerCoinsPerSecond) + "/s, drills " + NumberFormatter.Format(gm.Automation.DrillCoinsPerSecond) + "/s)\n" +
                               UIFactory.Colorize(tab == 0
                                   ? "Workers mine, collect and deliver ore for you. They work at " + NumberFormatter.Percent(gm.Stat(StatType.OfflineEfficiency)) + " speed while you're offline."
                                   : "Drills dig nonstop and keep running at full speed while you're offline (up to " + gm.Stat(StatType.OfflineHours).ToString("0.#") + "h).", UIFactory.TextDim);
            });

            ProducerDefinition[] list = tab == 0 ? gm.Db.workers.Cast<ProducerDefinition>().ToArray() : gm.Db.drills.Cast<ProducerDefinition>().ToArray();
            int ti = 0;
            foreach (var p in list)
            {
                int tier = ti++;
                Sprite icon = p is DrillDefinition
                    ? SpriteFactory.Drill(p.color, p.id.Length)
                    : SpriteFactory.Miner(p.color, Color.Lerp(p.color, Color.black, 0.5f), 0, p.id == "worker_robot" ? 1 : p.id == "worker_ai" ? 2 : 0);
                var row = Rows.Make(content, icon, p.displayName, p.description);
                var buy = Rows.Button(row, "", UIFactory.Gold * 0.8f + new Color(0, 0, 0, 0.2f), () => gm.Automation.Buy(p), 250);
                ui.AddRefresher(() =>
                {
                    int owned = gm.Automation.Owned(p);
                    bool unlocked = gm.Automation.IsUnlocked(p);
                    row.title.text = p.displayName + "  " + UIFactory.Colorize("x" + owned, UIFactory.Gold);
                    double eff = gm.Stat(p is DrillDefinition ? StatType.DrillEfficiency : StatType.WorkerEfficiency);
                    double each = p.ratePerSecond * eff * gm.Zones.ValueFactor(gm.Save.unlockedZone) * gm.Stat(StatType.OreValue) * gm.Stat(StatType.CoinMultiplier);
                    row.sub.text = p.description + "\n+" + NumberFormatter.Format(each) + " coins/s each" +
                                   (owned > 0 ? "  |  total +" + NumberFormatter.Format(each * owned) + "/s" : "");
                    if (!unlocked) buy.Set(gm.Automation.LockReason(p), false, UIFactory.Disabled);
                    else buy.Set("Buy\n" + NumberFormatter.Format(gm.Automation.Cost(p)), gm.Currency.CanAfford(CurrencyType.Coins, gm.Automation.Cost(p)), UIFactory.Gold * 0.8f + new Color(0, 0, 0, 0.2f));
                    if (row.icon != null) row.icon.color = unlocked ? Color.white : new Color(0.3f, 0.3f, 0.35f);
                });
            }
        }
    }

    /// <summary>Backpack contents, selling, and the ore encyclopedia.</summary>
    public class InventoryPanel : UIPanel
    {
        public override string Title => "Backpack";

        public override void Build(RectTransform content, int tab)
        {
            var head = Rows.Make(content, SpriteFactory.BagIcon, "Backpack", "");
            var sell = Rows.Button(head, "", UIFactory.Gold * 0.85f + new Color(0, 0, 0, 0.15f), () => { ui.SellPressed(); ui.Rebuild(); }, 300);
            ui.AddRefresher(() =>
            {
                head.title.text = "Backpack  " + UIFactory.Colorize(NumberFormatter.Format(gm.Inventory.TotalCount) + " / " + NumberFormatter.Format(gm.Inventory.Capacity), UIFactory.Gold);
                head.sub.gameObject.SetActive(true);
                head.sub.text = gm.Zones.AtStation
                    ? "You're at the Trading Post: full price!"
                    : "Selling away from the Trading Post pays " + NumberFormatter.Percent(gm.Inventory.RemoteRate) + ". Take the elevator to the Surface for 100%.";
                double v = gm.Inventory.TotalValue * ui.SellRate;
                sell.Set("Sell all\n+" + NumberFormatter.Format(v), gm.Inventory.TotalCount > 0);
            });

            var items = gm.Inventory.Snapshot();
            if (items.Count == 0) Rows.Paragraph(content, "Your backpack is empty. Click or tap rocks to mine them!", 26, UIFactory.TextDim);
            foreach (var e in items.OrderByDescending(x => gm.Db.GetOre(x.id)?.baseValue ?? 0))
            {
                var ore = gm.Db.GetOre(e.id);
                if (ore == null) continue;
                var row = Rows.Make(content, SpriteFactory.OreChunk(ore), ore.displayName, "", 100);
                row.title.text = ore.displayName + "  " + UIFactory.Colorize("x" + NumberFormatter.Format(e.count), UIFactory.Gold);
                row.sub.gameObject.SetActive(true);
                row.sub.text = NumberFormatter.Format(gm.Inventory.ValueOf(ore)) + " each  |  worth " + NumberFormatter.Format(gm.Inventory.ValueOf(ore) * e.count);
            }

            Rows.Header(content, "Ore Encyclopedia");
            foreach (var z in gm.Db.zones)
            {
                bool known = gm.Save.stats.deepestZoneEver >= z.index;
                string ores = string.Join(", ", z.oreIds.Select(id =>
                {
                    var o = gm.Db.GetOre(id);
                    if (o == null) return id;
                    return known ? UIFactory.Colorize(o.displayName, Color.Lerp(o.color, Color.white, 0.35f)) + " (" + NumberFormatter.Format(o.baseValue) + ")" : "???";
                }));
                Rows.Paragraph(content, UIFactory.Colorize(known ? z.displayName : "Undiscovered zone", UIFactory.TextMain) + ":  " + ores, 24, UIFactory.TextDim);
            }
        }
    }
}
