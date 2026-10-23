using System;
using UnityEngine;

namespace IdleMiningEmpire
{
    public struct Cosmetic
    {
        public string name;
        public int level;
        public Color head;    // pickaxe head colour
        public Color helmet;  // helmet colour
        public Color suit;    // outfit colour
    }

    /// <summary>Player XP and levels (max 999) with stat boosts, titles, cosmetics and feature unlocks.</summary>
    public class LevelManager : MonoBehaviour, IStatProvider
    {
        public const int MaxLevel = 999;
        GameManager gm;

        static readonly (int lvl, string title)[] Titles =
        {
            (1, "Rookie"), (5, "Digger"), (10, "Prospector"), (20, "Excavator"), (35, "Foreman"),
            (50, "Ore Baron"), (75, "Deep Delver"), (100, "Core Seeker"), (150, "Magnate"),
            (200, "Tycoon"), (300, "Earthshaker"), (400, "Relic Hunter"), (500, "Mining Mogul"),
            (650, "Abyss Walker"), (800, "Planet Eater"), (999, "Living Legend")
        };

        public static readonly Cosmetic[] Cosmetics =
        {
            new Cosmetic { name = "Wooden Pickaxe", level = 1, head = new Color(0.55f, 0.55f, 0.6f), helmet = new Color(1f, 0.8f, 0.15f), suit = new Color(0.25f, 0.45f, 0.85f) },
            new Cosmetic { name = "Copper Pickaxe", level = 10, head = new Color(0.85f, 0.47f, 0.23f), helmet = new Color(1f, 0.55f, 0.15f), suit = new Color(0.2f, 0.6f, 0.45f) },
            new Cosmetic { name = "Golden Pickaxe", level = 25, head = new Color(1f, 0.8f, 0.1f), helmet = new Color(0.95f, 0.95f, 0.95f), suit = new Color(0.65f, 0.2f, 0.25f) },
            new Cosmetic { name = "Emerald Pickaxe", level = 50, head = new Color(0.15f, 0.85f, 0.45f), helmet = new Color(0.15f, 0.2f, 0.25f), suit = new Color(0.35f, 0.3f, 0.6f) },
            new Cosmetic { name = "Diamond Pickaxe", level = 100, head = new Color(0.55f, 0.92f, 1f), helmet = new Color(0.3f, 0.75f, 1f), suit = new Color(0.12f, 0.15f, 0.3f) },
            new Cosmetic { name = "Plasma Pickaxe", level = 200, head = new Color(1f, 0.35f, 0.95f), helmet = new Color(0.6f, 0.2f, 0.9f), suit = new Color(0.1f, 0.1f, 0.12f) },
            new Cosmetic { name = "Alien Pickaxe", level = 400, head = new Color(0.25f, 1f, 0.2f), helmet = new Color(0.2f, 0.9f, 0.6f), suit = new Color(0.85f, 0.85f, 0.9f) },
            new Cosmetic { name = "Ancient Pickaxe", level = 750, head = new Color(0.35f, 1f, 0.95f), helmet = new Color(1f, 0.85f, 0.3f), suit = new Color(0.3f, 0.1f, 0.35f) },
        };

        public void Init(GameManager g) { gm = g; }

        public int Level => gm.Save.playerLevel;
        public double Xp => gm.Save.playerXp;

        public static double XpToNext(int level) => Math.Floor(25 * Math.Pow(level, 1.8));
        public double Progress => Level >= MaxLevel ? 1 : Xp / XpToNext(Level);

        public string Title
        {
            get
            {
                string t = Titles[0].title;
                foreach (var e in Titles) if (Level >= e.lvl) t = e.title;
                return t;
            }
        }

        public int BestCosmeticIndex
        {
            get
            {
                int best = 0;
                for (int i = 0; i < Cosmetics.Length; i++) if (Level >= Cosmetics[i].level) best = i;
                return best;
            }
        }

        public Cosmetic CurrentCosmetic
        {
            get
            {
                int sel = gm.Save.selectedCosmetic;
                if (sel < 0 || sel >= Cosmetics.Length || Cosmetics[sel].level > Level) sel = BestCosmeticIndex;
                return Cosmetics[sel];
            }
        }

        public void AddXp(double amount)
        {
            if (Level >= MaxLevel || amount <= 0) return;
            gm.Save.playerXp += amount * gm.Stat(StatType.XpMultiplier);
            int before = Level;
            while (Level < MaxLevel && gm.Save.playerXp >= XpToNext(Level))
            {
                gm.Save.playerXp -= XpToNext(Level);
                gm.Save.playerLevel++;
                OnLevelUp(gm.Save.playerLevel);
            }
            if (Level >= MaxLevel) gm.Save.playerXp = 0;
            if (Level != before)
            {
                gm.Audio.Play(Sfx.LevelUp);
                GameEvents.RaiseStatsChanged();
                GameEvents.RaiseLevelUp(Level);
                gm.Fx.FloatingText(gm.Player.transform.position + Vector3.up * 3.6f, "LEVEL UP! " + Level, new Color(0.5f, 1f, 0.6f), 1.6f);
            }
        }

        void OnLevelUp(int lvl)
        {
            double gems = lvl % 10 == 0 ? 10 : 2;
            gm.Currency.AddGems(gems);
            foreach (var e in Titles) if (e.lvl == lvl) GameEvents.RaiseToast("New title: " + e.title + "!");
            foreach (var c in Cosmetics) if (c.level == lvl && lvl > 1) GameEvents.RaiseToast("Cosmetic unlocked: " + c.name + "!");
            if (lvl == WorkersUnlockLevel) GameEvents.RaiseToast("Workers unlocked! Open the Shop.");
            if (lvl == PetManager.UnlockLevel) GameEvents.RaiseToast("Pets unlocked! Hatch an egg.");
            if (lvl == ResearchManager.UnlockLevel) GameEvents.RaiseToast("Research Tree unlocked!");
            if (lvl == DrillsUnlockLevel) GameEvents.RaiseToast("Drills unlocked! They work even offline.");
        }

        public const int WorkersUnlockLevel = 2;
        public const int DrillsUnlockLevel = 7;

        public void ContributeStats(StatSheet sheet)
        {
            int l = Level - 1;
            if (l <= 0) return;
            sheet.AddPercent(StatType.MiningPower, 0.02 * l);
            sheet.AddPercent(StatType.CoinMultiplier, 0.01 * l);
            sheet.AddPercent(StatType.XpMultiplier, 0.005 * l);
        }
    }
}
