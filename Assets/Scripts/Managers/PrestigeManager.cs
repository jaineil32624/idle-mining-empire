using System;
using UnityEngine;

namespace IdleMiningEmpire
{
    /// <summary>
    /// Rebirth: after reaching the Core, reset coins/ores/equipment/workers/zones for Ancient Crystals.
    /// Keeps crystals, crystal upgrades, gem upgrades, pets, research, level and cosmetics.
    /// </summary>
    public class PrestigeManager : MonoBehaviour, IStatProvider
    {
        public const int RequiredZone = 4;
        GameManager gm;

        public void Init(GameManager g) { gm = g; }

        public bool CanPrestige => gm.Save.unlockedZone >= RequiredZone;

        public double CrystalsOnPrestige
        {
            get
            {
                if (!CanPrestige) return 0;
                var s = gm.Save;
                double fromCoins = 5 * Math.Sqrt(Math.Max(0, s.coinsEarnedThisRun) / 1e9);
                double fromDepth = s.unlockedZone >= 5 ? 5 : 0;
                if (s.unlockedZone >= 6) fromDepth += 3 * s.abyssUnlockedDepth;
                double total = (fromCoins + fromDepth) * gm.Stat(StatType.CrystalGain);
                return Math.Max(1, Math.Floor(total));
            }
        }

        public int Count => (int)gm.Save.stats.prestiges;

        public void DoPrestige()
        {
            if (!CanPrestige) { gm.Audio.Play(Sfx.Error); return; }
            double gain = CrystalsOnPrestige;
            var s = gm.Save;

            gm.Currency.AddCrystals(gain);
            s.stats.prestiges++;

            s.inventory.Clear();
            gm.Upgrades.ResetEquipment();
            s.workers.Clear();
            s.drills.Clear();
            s.unlockedZone = 0;
            s.currentZone = 0;
            s.abyssDepth = 1;
            s.abyssUnlockedDepth = 1;
            s.coinsEarnedThisRun = 0;
            s.workersHiredThisRun = 0;
            GameEvents.RaiseStatsChanged();
            s.coins = Math.Floor(gm.Stat(StatType.StartingCoins));

            gm.Zones.EnterZone(0, true);
            gm.Audio.Play(Sfx.Prestige);
            gm.Fx.Flash(new Color(0.4f, 1f, 1f, 0.9f));
            GameEvents.RaisePrestige();
            GameEvents.RaiseToast("Reborn! +" + NumberFormatter.Format(gain) + " Ancient Crystals");
            gm.Saves.SaveNow();
        }

        public void ContributeStats(StatSheet sheet)
        {
            double total = gm.Save.totalCrystalsEarned;
            if (total > 0)
            {
                sheet.AddPercent(StatType.CoinMultiplier, 0.02 * total);
                sheet.AddPercent(StatType.MiningPower, 0.02 * total);
            }
            double p = gm.Save.stats.prestiges;
            if (p > 0)
            {
                sheet.AddPercent(StatType.OreValue, 0.1 * p);
                sheet.AddPercent(StatType.XpMultiplier, 0.1 * p);
            }
        }
    }
}
