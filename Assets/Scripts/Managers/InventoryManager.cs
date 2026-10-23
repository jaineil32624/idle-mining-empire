using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleMiningEmpire
{
    /// <summary>The player's backpack of ores, plus ore pricing and selling.</summary>
    public class InventoryManager : MonoBehaviour
    {
        GameManager gm;
        SaveData S => gm.Save;

        public void Init(GameManager g) { gm = g; }

        public int Capacity => (int)Math.Floor(gm.Stat(StatType.BackpackCapacity));

        public double TotalCount
        {
            get
            {
                double t = 0;
                foreach (var e in S.inventory) t += e.count;
                return t;
            }
        }

        public bool IsFull => TotalCount >= Capacity;
        public double FreeSpace => Math.Max(0, Capacity - TotalCount);

        public double Count(string oreId)
        {
            foreach (var e in S.inventory) if (e.id == oreId) return e.count;
            return 0;
        }

        /// <summary>Coin value of a single ore after every multiplier.</summary>
        public double ValueOf(OreDefinition ore)
        {
            double v = ore.baseValue;
            if (ore.scalesWithAbyss) v *= Math.Pow(1.45, Math.Max(0, S.abyssUnlockedDepth - 1));
            return v * gm.Stat(StatType.OreValue) * gm.Stat(StatType.CoinMultiplier);
        }

        public double TotalValue
        {
            get
            {
                double t = 0;
                foreach (var e in S.inventory)
                {
                    var o = gm.Db.GetOre(e.id);
                    if (o != null) t += ValueOf(o) * e.count;
                }
                return t;
            }
        }

        /// <summary>Adds up to n ores; returns how many fit.</summary>
        public double TryAdd(OreDefinition ore, double n)
        {
            double free = FreeSpace;
            if (free <= 0 && gm.Stat(StatType.AutoSell) > 0)
            {
                SellAll(RemoteRate, true);
                free = FreeSpace;
            }
            double add = Math.Min(free, n);
            if (add <= 0) return 0;
            bool found = false;
            foreach (var e in S.inventory)
                if (e.id == ore.id) { e.count += add; found = true; break; }
            if (!found) S.inventory.Add(new IdCount(ore.id, add));
            S.stats.oresMined += add;
            GameEvents.RaiseOreCollected(ore, add);
            return add;
        }

        public double RemoteRate => gm.Stat(StatType.AutoSellBonus);

        /// <summary>Sells everything at the given price rate (1 = full price). Returns coins earned.</summary>
        public double SellAll(double rate, bool silent = false)
        {
            double total = TotalValue * rate;
            double count = TotalCount;
            if (count <= 0) return 0;
            S.inventory.Clear();
            gm.Currency.AddCoins(total);
            gm.Audio.Play(Sfx.Sell);
            if (!silent || total > 0)
                gm.Fx.FloatingText(gm.Player.transform.position + Vector3.up * 3.2f,
                    "+" + NumberFormatter.Format(total), new Color(1f, 0.85f, 0.2f), 1.4f);
            return total;
        }

        public List<IdCount> Snapshot() => new List<IdCount>(S.inventory);
    }
}
