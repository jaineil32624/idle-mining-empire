using System;

namespace IdleMiningEmpire
{
    [Serializable]
    public struct StatModifier
    {
        public StatType stat;
        public ModifierMode mode;
        public double value;
        public double expBase;

        public StatModifier(StatType stat, ModifierMode mode, double value, double expBase = 1.0)
        {
            this.stat = stat;
            this.mode = mode;
            this.value = value;
            this.expBase = expBase;
        }

        public static StatModifier Flat(StatType s, double v) => new StatModifier(s, ModifierMode.Flat, v);
        public static StatModifier Pct(StatType s, double v) => new StatModifier(s, ModifierMode.Percent, v);

        /// <summary>Applies this modifier, scaled by an integer level, onto a stat sheet.</summary>
        public void Apply(StatSheet sheet, int level)
        {
            if (level <= 0) return;
            switch (mode)
            {
                case ModifierMode.Flat:
                    sheet.AddFlat(stat, value * level);
                    break;
                case ModifierMode.Percent:
                    sheet.AddPercent(stat, value * level);
                    break;
                case ModifierMode.Curve:
                    sheet.AddFlat(stat, value * ((1 + level) * Math.Pow(expBase, level) - 1));
                    break;
                case ModifierMode.Quadratic:
                    sheet.AddFlat(stat, value * level * (1 + level / 10.0));
                    break;
                case ModifierMode.Exponential:
                    sheet.AddFlat(stat, value * (Math.Pow(expBase, level) - 1));
                    break;
            }
        }

        /// <summary>Short human readable description for a given level, e.g. "+12% Luck".</summary>
        public string Describe(int level)
        {
            var tmp = new StatSheet();
            Apply(tmp, Math.Max(1, level));
            double flat = tmp.GetFlat(stat), pct = tmp.GetPercent(stat);
            string name = StatInfo.Name(stat);
            if (pct != 0) return (pct > 0 ? "+" : "") + NumberFormatter.Percent(pct) + " " + name;
            return StatInfo.FormatFlat(stat, flat) + " " + name;
        }
    }
}
