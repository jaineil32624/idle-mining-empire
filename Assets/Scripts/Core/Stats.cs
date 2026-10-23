using System;

namespace IdleMiningEmpire
{
    /// <summary>Accumulates flat / percent / multiplicative modifiers for every StatType.</summary>
    public class StatSheet
    {
        const int N = (int)StatType.Count;
        readonly double[] flat = new double[N];
        readonly double[] pct = new double[N];
        readonly double[] mult = new double[N];

        public StatSheet() { Clear(); }

        public void Clear()
        {
            for (int i = 0; i < N; i++) { flat[i] = 0; pct[i] = 0; mult[i] = 1; }
        }

        public void AddFlat(StatType s, double v) => flat[(int)s] += v;
        public void AddPercent(StatType s, double v) => pct[(int)s] += v;
        public void AddMultiplier(StatType s, double v) => mult[(int)s] *= v;
        public double GetFlat(StatType s) => flat[(int)s];
        public double GetPercent(StatType s) => pct[(int)s];

        public double Final(StatType s)
        {
            int i = (int)s;
            double v = (StatInfo.Base(s) + flat[i]) * Math.Max(0, 1 + pct[i]) * mult[i];
            return StatInfo.Clamp(s, v);
        }
    }

    /// <summary>Base values, names and limits for stats.</summary>
    public static class StatInfo
    {
        public static double Base(StatType s)
        {
            switch (s)
            {
                case StatType.MiningPower: return 1;
                case StatType.CritChance: return 0.05;
                case StatType.CritDamage: return 2.0;
                case StatType.MiningSpeed: return 2.5;
                case StatType.Luck: return 1;
                case StatType.BackpackCapacity: return 25;
                case StatType.AutoSellBonus: return 0.5;
                case StatType.OreMultiplier: return 1;
                case StatType.WalkSpeed: return 5.5;
                case StatType.DashCooldown: return 2.0;
                case StatType.JumpHeight: return 2.2;
                case StatType.OreValue: return 1;
                case StatType.CoinMultiplier: return 1;
                case StatType.WorkerEfficiency: return 1;
                case StatType.DrillEfficiency: return 1;
                case StatType.OfflineHours: return 4;
                case StatType.OfflineEfficiency: return 0.5;
                case StatType.XpMultiplier: return 1;
                case StatType.GemChance: return 0.004;
                case StatType.PetSlots: return 3;
                case StatType.AutoMine: return 0;
                case StatType.AutoSell: return 0;
                case StatType.Splash: return 0;
                case StatType.RockCount: return 7;
                case StatType.MineRange: return 3.2;
                case StatType.CrystalGain: return 1;
                case StatType.StartingCoins: return 0;
                case StatType.PetXp: return 1;
                default: return 0;
            }
        }

        public static double Clamp(StatType s, double v)
        {
            switch (s)
            {
                case StatType.CritChance: return Math.Min(0.9, Math.Max(0, v));
                case StatType.AutoSellBonus: return Math.Min(1.0, Math.Max(0, v));
                case StatType.DashCooldown: return Math.Max(0.25, v);
                case StatType.MiningSpeed: return Math.Min(30, v);
                case StatType.WalkSpeed: return Math.Min(25, v);
                case StatType.JumpHeight: return Math.Min(9, v);
                case StatType.OfflineEfficiency: return Math.Min(1.5, v);
                case StatType.GemChance: return Math.Min(0.25, v);
                case StatType.PetSlots: return Math.Min(8, Math.Floor(v));
                case StatType.RockCount: return Math.Min(16, Math.Floor(v));
                case StatType.Splash: return Math.Min(1, v);
                default: return v;
            }
        }

        public static string Name(StatType s)
        {
            switch (s)
            {
                case StatType.MiningPower: return "Mining Power";
                case StatType.CritChance: return "Crit Chance";
                case StatType.CritDamage: return "Crit Damage";
                case StatType.MiningSpeed: return "Mining Speed";
                case StatType.Luck: return "Luck";
                case StatType.BackpackCapacity: return "Backpack Size";
                case StatType.AutoSellBonus: return "Remote Sell Rate";
                case StatType.OreMultiplier: return "Ore Multiplier";
                case StatType.WalkSpeed: return "Walk Speed";
                case StatType.DashCooldown: return "Dash Cooldown";
                case StatType.JumpHeight: return "Jump Height";
                case StatType.OreValue: return "Ore Value";
                case StatType.CoinMultiplier: return "Coin Multiplier";
                case StatType.WorkerEfficiency: return "Worker Output";
                case StatType.DrillEfficiency: return "Drill Output";
                case StatType.OfflineHours: return "Max Offline Hours";
                case StatType.OfflineEfficiency: return "Offline Worker Rate";
                case StatType.XpMultiplier: return "XP Gain";
                case StatType.GemChance: return "Gem Find Chance";
                case StatType.PetSlots: return "Pet Slots";
                case StatType.AutoMine: return "Auto-Mine";
                case StatType.AutoSell: return "Auto-Sell when full";
                case StatType.Splash: return "Splash Damage";
                case StatType.RockCount: return "Rocks per Zone";
                case StatType.MineRange: return "Mining Reach";
                case StatType.CrystalGain: return "Crystal Gain";
                case StatType.StartingCoins: return "Starting Coins";
                case StatType.PetXp: return "Pet XP";
                default: return s.ToString();
            }
        }

        public static string FormatFlat(StatType s, double v)
        {
            string sign = v >= 0 ? "+" : "";
            switch (s)
            {
                case StatType.CritChance:
                case StatType.AutoSellBonus:
                case StatType.GemChance:
                case StatType.Splash:
                case StatType.OfflineEfficiency:
                    return sign + NumberFormatter.Percent(v);
                case StatType.CritDamage:
                    return sign + v.ToString("0.##") + "x";
                case StatType.DashCooldown:
                    return sign + v.ToString("0.##") + "s";
                case StatType.AutoMine:
                case StatType.AutoSell:
                    return "Unlocks";
                default:
                    return sign + NumberFormatter.Format(v);
            }
        }

        public static string FormatFinal(StatType s, double v)
        {
            switch (s)
            {
                case StatType.CritChance:
                case StatType.AutoSellBonus:
                case StatType.GemChance:
                case StatType.Splash:
                case StatType.OfflineEfficiency:
                    return NumberFormatter.Percent(v);
                case StatType.CritDamage:
                case StatType.Luck:
                case StatType.OreValue:
                case StatType.CoinMultiplier:
                case StatType.OreMultiplier:
                case StatType.WorkerEfficiency:
                case StatType.DrillEfficiency:
                case StatType.XpMultiplier:
                case StatType.CrystalGain:
                case StatType.PetXp:
                    return NumberFormatter.Mult(v);
                case StatType.DashCooldown: return v.ToString("0.00") + "s";
                case StatType.MiningSpeed: return v.ToString("0.0") + " hits/s";
                case StatType.WalkSpeed:
                case StatType.JumpHeight:
                case StatType.MineRange:
                    return v.ToString("0.0");
                case StatType.AutoMine:
                case StatType.AutoSell:
                    return v > 0 ? "ON" : "Locked";
                case StatType.OfflineHours: return v.ToString("0.#") + "h";
                default: return NumberFormatter.Format(v);
            }
        }
    }
}
