using System.Collections.Generic;
using UnityEngine;

namespace IdleMiningEmpire
{
    /// <summary>Pickaxe, Backpack, Movement (coins), Premium (gems) and Prestige (crystal) upgrades.</summary>
    public class UpgradeManager : MonoBehaviour, IStatProvider
    {
        GameManager gm;

        public void Init(GameManager g) { gm = g; }

        public int Level(UpgradeDefinition u) => SaveData.GetLevel(gm.Save.upgrades, u.id);

        public double Cost(UpgradeDefinition u) => u.CostAt(Level(u));

        public bool CanBuy(UpgradeDefinition u)
        {
            int l = Level(u);
            return !u.IsMaxed(l) && gm.Currency.CanAfford(u.currency, u.CostAt(l));
        }

        public bool Buy(UpgradeDefinition u)
        {
            int l = Level(u);
            if (u.IsMaxed(l)) return false;
            if (!gm.Currency.TrySpend(u.currency, u.CostAt(l)))
            {
                gm.Audio.Play(Sfx.Error);
                return false;
            }
            SaveData.SetLevel(gm.Save.upgrades, u.id, l + 1);
            gm.Audio.Play(Sfx.Buy);
            GameEvents.RaiseStatsChanged();
            GameEvents.RaiseUpgradeBought(u, l + 1);
            return true;
        }

        /// <summary>Buys as many levels as affordable (max 1000). Returns levels bought.</summary>
        public int BuyMax(UpgradeDefinition u)
        {
            int n = 0;
            while (n < 1000 && CanBuy(u))
            {
                int l = Level(u);
                gm.Currency.TrySpend(u.currency, u.CostAt(l));
                SaveData.SetLevel(gm.Save.upgrades, u.id, l + 1);
                n++;
            }
            if (n > 0)
            {
                gm.Audio.Play(Sfx.Buy);
                GameEvents.RaiseStatsChanged();
                GameEvents.RaiseUpgradeBought(u, Level(u));
            }
            else gm.Audio.Play(Sfx.Error);
            return n;
        }

        public IEnumerable<UpgradeDefinition> InCategory(UpgradeCategory c)
        {
            foreach (var u in gm.Db.upgrades) if (u.category == c) yield return u;
        }

        public void ContributeStats(StatSheet sheet)
        {
            foreach (var u in gm.Db.upgrades)
            {
                int l = Level(u);
                if (l > 0) u.modifier.Apply(sheet, l);
            }
        }

        /// <summary>Called on Rebirth: reset coin-bought equipment, keep premium and prestige upgrades.</summary>
        public void ResetEquipment()
        {
            gm.Save.upgrades.RemoveAll(e =>
            {
                var d = gm.Db.GetUpgrade(e.id);
                return d == null || d.currency == CurrencyType.Coins;
            });
            GameEvents.RaiseStatsChanged();
        }
    }
}
