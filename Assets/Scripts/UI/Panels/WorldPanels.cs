using System.Linq;
using UnityEngine;

namespace IdleMiningEmpire
{
    /// <summary>The elevator: unlock and travel between zones, and descend the infinite Abyss.</summary>
    public class ZonesPanel : UIPanel
    {
        public override string Title => "Elevator";

        public override void Build(RectTransform content, int tab)
        {
            Rows.Paragraph(content, "Each layer is harder and richer. Unlocking requires enough Mining Power and coins. The Trading Post (full price) is on the Surface.", 24, UIFactory.TextDim);
            foreach (var z in gm.Db.zones)
            {
                var zone = z;
                var oreCols = zone.oreIds.Select(id => gm.Db.GetOre(id)).Where(o => o != null).Select(o => o.color).ToArray();
                var row = Rows.Make(content, SpriteFactory.Rock(zone, oreCols, Rarity.Common, 0), zone.displayName, "", 150,
                    gm.Save.currentZone == zone.index ? new Color(0.16f, 0.26f, 0.32f) : (Color?)null);
                var btn = Rows.Button(row, "", UIFactory.Blue, () => Act(zone), 260, 100);
                UIFactory.UIButton descend = null;
                if (zone.isAbyss) descend = Rows.Button(row, "", UIFactory.Purple, () => { gm.Zones.TryDescend(); ui.Close(); }, 240, 100);

                ui.AddRefresher(() =>
                {
                    bool unlocked = gm.Zones.IsUnlocked(zone.index);
                    bool known = unlocked || zone.index == gm.Save.unlockedZone + 1;
                    bool here = gm.Save.currentZone == zone.index;
                    row.title.text = (known ? zone.displayName : "???") + (here ? UIFactory.Colorize("  (you are here)", UIFactory.Green) : "");
                    row.sub.gameObject.SetActive(true);
                    string ores = string.Join(", ", zone.oreIds.Select(id => gm.Db.GetOre(id)?.displayName ?? id));
                    string req = "Needs " + NumberFormatter.Format(zone.hardness) + " power" +
                                 (zone.requiresPrestiges > 0 ? ", " + zone.requiresPrestiges + " Rebirth" : "");
                    row.sub.text = known
                        ? zone.description + "\nOres: " + ores + "   |   " + req +
                          (zone.isAbyss && unlocked ? "\nDeepest depth: " + gm.Save.abyssUnlockedDepth + "  (bosses every 10 depths)" : "")
                        : "Unlock the previous zone to learn more.";
                    if (row.icon != null) row.icon.color = known ? Color.white : new Color(0.1f, 0.1f, 0.12f);

                    if (here) btn.Set("Here", false, UIFactory.Green);
                    else if (unlocked) btn.Set("Travel", true, UIFactory.Blue);
                    else
                    {
                        string block = gm.Zones.UnlockBlocker(zone.index);
                        btn.Set(block == null ? "Unlock\n" + NumberFormatter.Format(zone.unlockCost) : block, block == null, UIFactory.Gold * 0.8f + new Color(0, 0, 0, 0.2f));
                    }
                    if (descend != null)
                    {
                        descend.button.gameObject.SetActive(unlocked);
                        if (unlocked)
                        {
                            string b = gm.Zones.DescendBlocker();
                            descend.Set(b == null ? "Descend\n" + NumberFormatter.Format(gm.Zones.DescendCost) : b, b == null, UIFactory.Purple);
                        }
                    }
                });
            }
        }

        void Act(ZoneDefinition z)
        {
            if (gm.Zones.IsUnlocked(z.index))
            {
                gm.Zones.EnterZone(z.index, false);
                ui.Close();
            }
            else if (gm.Zones.TryUnlock(z.index))
            {
                ui.Close();
                gm.Fx.Confetti();
            }
        }
    }

    /// <summary>Rebirth and the Ancient Crystal shop.</summary>
    public class PrestigePanel : UIPanel
    {
        public override string Title => "Rebirth";
        public override string[] Tabs => new[] { "Rebirth", "Crystal Shop" };

        public override void Build(RectTransform content, int tab)
        {
            if (tab == 1)
            {
                var bal = Rows.Paragraph(content, "", 28);
                ui.AddRefresher(() => bal.text = "Ancient Crystals: " + UIFactory.Colorize(NumberFormatter.Format(gm.Currency.Crystals), new Color(0.4f, 1f, 1f)) +
                                                "   " + UIFactory.Colorize("Crystal upgrades are permanent.", UIFactory.TextDim));
                foreach (var u in gm.Upgrades.InCategory(UpgradeCategory.Prestige).ToList()) UpgradesPanel.AddUpgradeRow(gm, ui, content, u);
                return;
            }

            var info = Rows.Paragraph(content, "", 27);
            ui.AddRefresher(() =>
            {
                double total = gm.Save.totalCrystalsEarned;
                info.text =
                    "Reach the " + UIFactory.Colorize("Earth's Core", UIFactory.Gold) + " to Rebirth.\n\n" +
                    UIFactory.Colorize("Resets:", UIFactory.Red) + " coins, ores, coin upgrades, workers, drills and zone unlocks.\n" +
                    UIFactory.Colorize("Keeps:", UIFactory.Green) + " Ancient Crystals, crystal & gem upgrades, research, pets, level, gems and cosmetics.\n\n" +
                    "Every crystal ever earned gives +2% coins and +2% mining power. Each Rebirth adds +10% ore value and XP.\n" +
                    "Your first Rebirth reveals the " + UIFactory.Colorize("Ancient Ruins", new Color(0.4f, 1f, 0.9f)) + " and the endless " + UIFactory.Colorize("Abyss", UIFactory.Purple) + ".\n\n" +
                    "Rebirths: " + gm.Prestige.Count + "   |   Crystals earned all-time: " + NumberFormatter.Format(total) +
                    "   |   Passive bonus: +" + NumberFormatter.Percent(total * 0.02) + "\n" +
                    "Coins earned this run: " + NumberFormatter.Format(gm.Save.coinsEarnedThisRun);
            });

            var row = Rows.Make(content, SpriteFactory.CrystalIcon, "Rebirth now", "", 150, new Color(0.1f, 0.25f, 0.3f));
            var btn = Rows.Button(row, "", new Color(0.15f, 0.7f, 0.8f), () =>
            {
                ui.ShowConfirm("Rebirth?", "You will gain " + NumberFormatter.Format(gm.Prestige.CrystalsOnPrestige) +
                                           " Ancient Crystals and restart from the Surface, much stronger than before.",
                    "Rebirth!", () => { ui.Close(); gm.Prestige.DoPrestige(); }, new Color(0.15f, 0.7f, 0.8f));
            }, 300, 110);
            ui.AddRefresher(() =>
            {
                bool can = gm.Prestige.CanPrestige;
                row.title.text = can ? "Rebirth for " + UIFactory.Colorize("+" + NumberFormatter.Format(gm.Prestige.CrystalsOnPrestige) + " Crystals", new Color(0.4f, 1f, 1f)) : "Rebirth locked";
                row.sub.gameObject.SetActive(true);
                row.sub.text = can ? "More coins earned this run and deeper Abyss depths = more crystals." : "Unlock the Core Layer first.";
                btn.Set(can ? "REBIRTH" : "Locked", can, new Color(0.15f, 0.7f, 0.8f));
            });
        }
    }
}
