using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace IdleMiningEmpire
{
    public class EggType
    {
        public string name;
        public CurrencyType currency;
        public float[] odds; // Common..Mythic, sums to 100
        public Color color;
    }

    /// <summary>Hatching, equipping and auto-levelling pets.</summary>
    public class PetManager : MonoBehaviour, IStatProvider
    {
        public const int UnlockLevel = 3;
        public const int MaxPets = 60;
        public const int MaxPetLevel = 100;

        GameManager gm;
        public readonly EggType[] Eggs =
        {
            new EggType { name = "Miner's Egg", currency = CurrencyType.Coins, odds = new[] { 70f, 22f, 6.5f, 1.3f, 0.2f }, color = new Color(0.95f, 0.85f, 0.6f) },
            new EggType { name = "Gem Egg", currency = CurrencyType.Gems, odds = new[] { 40f, 35f, 18f, 6f, 1f }, color = new Color(1f, 0.45f, 0.85f) },
            new EggType { name = "Ancient Egg", currency = CurrencyType.Gems, odds = new[] { 0f, 40f, 35f, 20f, 5f }, color = new Color(0.4f, 1f, 0.95f) },
        };

        public void Init(GameManager g)
        {
            gm = g;
            GameEvents.RockBroken += OnRockBroken;
        }

        public bool FeatureUnlocked => gm.Save.playerLevel >= UnlockLevel;
        public List<PetInstance> All => gm.Save.pets;
        public IEnumerable<PetInstance> Equipped => gm.Save.pets.Where(p => p.equipped);
        public int EquippedCount => gm.Save.pets.Count(p => p.equipped);
        public int Slots => (int)gm.Stat(StatType.PetSlots);

        public double EggCost(int eggIndex)
        {
            switch (eggIndex)
            {
                case 0:
                    int z = Mathf.Clamp(gm.Save.unlockedZone, 0, 5);
                    return Math.Ceiling(300 * Math.Pow(30, z));
                case 1: return 40;
                default: return 200;
            }
        }

        public PetInstance Hatch(int eggIndex)
        {
            if (!FeatureUnlocked) { GameEvents.RaiseToast("Pets unlock at level " + UnlockLevel); return null; }
            if (gm.Save.pets.Count >= MaxPets) { GameEvents.RaiseToast("Pet storage full! Release some pets."); gm.Audio.Play(Sfx.Error); return null; }
            var egg = Eggs[eggIndex];
            if (!gm.Currency.TrySpend(egg.currency, EggCost(eggIndex))) { gm.Audio.Play(Sfx.Error); return null; }

            Rarity r = RollRarity(egg.odds);
            var candidates = gm.Db.pets.Where(p => p.rarity == r).ToList();
            if (candidates.Count == 0) candidates = gm.Db.pets;
            var def = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            var inst = new PetInstance { uid = Guid.NewGuid().ToString("N").Substring(0, 10), defId = def.id };
            gm.Save.pets.Add(inst);
            if (EquippedCount < Slots) inst.equipped = true;

            gm.Save.stats.petsHatched++;
            if (r == Rarity.Mythic) gm.Save.stats.mythicPets++;
            gm.Audio.Play(Sfx.Hatch);
            GameEvents.RaiseStatsChanged();
            GameEvents.RaisePetHatched(inst);
            return inst;
        }

        Rarity RollRarity(float[] odds)
        {
            // Luck nudges egg odds a little toward rarer pets.
            double luck = Math.Min(3, Math.Sqrt(gm.Stat(StatType.Luck)));
            double[] w = new double[5];
            double total = 0;
            for (int i = 0; i < 5; i++) { w[i] = odds[i] * Math.Pow(luck, i * 0.5); total += w[i]; }
            double roll = UnityEngine.Random.value * total;
            for (int i = 0; i < 5; i++) { roll -= w[i]; if (roll <= 0) return (Rarity)i; }
            return Rarity.Common;
        }

        public PetDefinition Def(PetInstance p) => gm.Db.GetPet(p.defId);

        public double Bonus(PetInstance p)
        {
            var d = Def(p);
            if (d == null) return 0;
            return d.bonusPercent * (1 + 0.06 * (p.level - 1));
        }

        public static double XpToNext(int level) => Math.Floor(10 * Math.Pow(level, 1.5));

        public void SetEquipped(PetInstance p, bool on)
        {
            if (on && !p.equipped && EquippedCount >= Slots) { GameEvents.RaiseToast("All pet slots are full."); gm.Audio.Play(Sfx.Error); return; }
            p.equipped = on;
            gm.Audio.Play(Sfx.Click);
            GameEvents.RaiseStatsChanged();
        }

        public void EquipBest()
        {
            foreach (var p in gm.Save.pets) p.equipped = false;
            var best = gm.Save.pets.OrderByDescending(p => Bonus(p) * StatWeight(Def(p))).Take(Slots);
            foreach (var p in best) p.equipped = true;
            gm.Audio.Play(Sfx.Buy);
            GameEvents.RaiseStatsChanged();
        }

        static double StatWeight(PetDefinition d)
        {
            if (d == null) return 0;
            switch (d.bonusStat)
            {
                case StatType.CoinMultiplier: return 1.2;
                case StatType.OreValue: return 1.15;
                case StatType.MiningSpeed: return 1.1;
                case StatType.Luck: return 1.0;
                default: return 0.8;
            }
        }

        public double ReleaseValue(PetInstance p)
        {
            var d = Def(p);
            int r = d != null ? (int)d.rarity : 0;
            return (r + 1) * (r + 1) + p.level / 10;
        }

        public void Release(PetInstance p)
        {
            gm.Currency.AddGems(ReleaseValue(p));
            gm.Save.pets.Remove(p);
            gm.Audio.Play(Sfx.Sell);
            GameEvents.RaiseStatsChanged();
        }

        void OnRockBroken(Rarity r, bool boss)
        {
            double xp = (1 + (int)r * 2 + (boss ? 50 : 0)) * (1 + gm.Save.currentZone * 0.5) * gm.Stat(StatType.PetXp);
            bool leveled = false;
            foreach (var p in gm.Save.pets)
            {
                if (!p.equipped || p.level >= MaxPetLevel) continue;
                p.xp += xp;
                while (p.level < MaxPetLevel && p.xp >= XpToNext(p.level))
                {
                    p.xp -= XpToNext(p.level);
                    p.level++;
                    leveled = true;
                }
            }
            if (leveled) GameEvents.RaiseStatsChanged();
        }

        public void ContributeStats(StatSheet sheet)
        {
            foreach (var p in gm.Save.pets)
            {
                if (!p.equipped) continue;
                var d = Def(p);
                if (d != null) sheet.AddPercent(d.bonusStat, Bonus(p));
            }
        }

        public static Color RarityColor(Rarity r)
        {
            switch (r)
            {
                case Rarity.Rare: return new Color(0.35f, 0.65f, 1f);
                case Rarity.Epic: return new Color(0.75f, 0.4f, 1f);
                case Rarity.Legendary: return new Color(1f, 0.6f, 0.15f);
                case Rarity.Mythic: return new Color(1f, 0.3f, 0.55f);
                default: return new Color(0.8f, 0.82f, 0.85f);
            }
        }
    }
}
