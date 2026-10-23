using System;
using System.Globalization;

namespace IdleMiningEmpire
{
    /// <summary>
    /// Formats very large numbers for idle-game display: 1.25K, 3.4M, 7.1Qa, 2.0ab ...
    /// All game currency is stored as double, which comfortably reaches 1e308.
    /// </summary>
    public static class NumberFormatter
    {
        static readonly string[] Suffixes =
        {
            "", "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "Dc",
            "UDc", "DDc", "TDc", "QaDc", "QiDc", "SxDc", "SpDc", "OcDc", "NoDc", "Vg"
        };

        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static string Format(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return "∞";
            if (value < 0) return "-" + Format(-value);
            if (value < 1000)
            {
                if (value < 10 && Math.Abs(value - Math.Round(value)) > 0.001)
                    return value.ToString("0.#", Inv);
                return Math.Floor(value).ToString("0", Inv);
            }

            int tier = (int)Math.Floor(Math.Log10(value) / 3.0);
            double scaled = value / Math.Pow(1000, tier);
            if (scaled >= 999.995) { scaled /= 1000; tier++; }

            string suffix;
            if (tier < Suffixes.Length) suffix = Suffixes[tier];
            else
            {
                // aa, ab, ac ... for absurd numbers
                int t = tier - Suffixes.Length;
                char a = (char)('a' + (t / 26) % 26);
                char b = (char)('a' + t % 26);
                suffix = new string(new[] { a, b });
            }

            string fmt = scaled < 10 ? "0.00" : scaled < 100 ? "0.0" : "0";
            return scaled.ToString(fmt, Inv) + suffix;
        }

        public static string Percent(double fraction) => (fraction * 100).ToString("0.#", Inv) + "%";

        public static string Mult(double m) => "x" + (m < 100 ? m.ToString("0.##", Inv) : Format(m));

        public static string Time(double seconds)
        {
            if (seconds < 60) return Math.Max(0, Math.Floor(seconds)) + "s";
            var ts = TimeSpan.FromSeconds(seconds);
            if (ts.TotalHours >= 1) return (int)ts.TotalHours + "h " + ts.Minutes + "m";
            return ts.Minutes + "m " + ts.Seconds + "s";
        }
    }
}
