using UnityEngine;

namespace IdleMiningEmpire
{
    /// <summary>Coins (standard), Gems (premium) and Ancient Crystals (prestige).</summary>
    public class CurrencyManager : MonoBehaviour
    {
        GameManager gm;
        SaveData S => gm.Save;

        public double Coins => S.coins;
        public double Gems => S.gems;
        public double Crystals => S.crystals;

        public void Init(GameManager g) { gm = g; }

        public double Get(CurrencyType t)
        {
            switch (t)
            {
                case CurrencyType.Gems: return S.gems;
                case CurrencyType.Crystals: return S.crystals;
                default: return S.coins;
            }
        }

        public bool CanAfford(CurrencyType t, double amount) => Get(t) >= amount - 1e-9;

        public bool TrySpend(CurrencyType t, double amount)
        {
            if (amount <= 0) return true;
            if (!CanAfford(t, amount)) return false;
            switch (t)
            {
                case CurrencyType.Gems: S.gems -= amount; break;
                case CurrencyType.Crystals: S.crystals -= amount; break;
                default: S.coins -= amount; break;
            }
            return true;
        }

        /// <summary>Adds coins. If 'earned', the amount counts toward quests, stats and prestige.</summary>
        public void AddCoins(double amount, bool earned = true)
        {
            if (amount <= 0 || double.IsNaN(amount)) return;
            S.coins += amount;
            if (!earned) return;
            S.coinsEarnedThisRun += amount;
            S.stats.coinsEarned += amount;
            GameEvents.RaiseCoinsEarned(amount);
        }

        public void AddGems(double amount)
        {
            if (amount <= 0) return;
            S.gems += amount;
        }

        public void AddCrystals(double amount)
        {
            if (amount <= 0) return;
            S.crystals += amount;
            S.totalCrystalsEarned += amount;
            GameEvents.RaiseStatsChanged();
        }

        public static string Symbol(CurrencyType t)
        {
            switch (t)
            {
                case CurrencyType.Gems: return "Gems";
                case CurrencyType.Crystals: return "Crystals";
                default: return "Coins";
            }
        }

        public static Color ColorOf(CurrencyType t)
        {
            switch (t)
            {
                case CurrencyType.Gems: return new Color(1f, 0.42f, 0.85f);
                case CurrencyType.Crystals: return new Color(0.35f, 0.95f, 1f);
                default: return new Color(1f, 0.82f, 0.2f);
            }
        }
    }
}
