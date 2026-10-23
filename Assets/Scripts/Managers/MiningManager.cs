using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleMiningEmpire
{
    /// <summary>Spawns rocks by rarity, resolves hits (crits, splash, damage numbers) and drops ores, gems, XP and boss loot.</summary>
    public class MiningManager : MonoBehaviour
    {
        static readonly double[] RarityWeights = { 75, 18, 5.5, 1.3, 0.2 };
        static readonly double[] RarityHp = { 1, 2.5, 6, 15, 40 };
        static readonly double[] RarityDrops = { 1, 2.5, 6, 15, 50 };
        static readonly double[] RarityXp = { 1, 3, 10, 40, 200 };
        static readonly float[][] OreWeightsByRarity =
        {
            new[] { 60f, 30f, 10f },
            new[] { 40f, 40f, 20f },
            new[] { 20f, 45f, 35f },
            new[] { 10f, 40f, 50f },
            new[] { 0f, 30f, 70f },
        };

        public const float RockMinX = -8.5f;
        GameManager gm;
        readonly List<Rock> rocks = new List<Rock>();
        readonly List<OrePickup> pickups = new List<OrePickup>();
        readonly List<float> respawnTimers = new List<float>();
        float fullNoticeCooldown;

        public IReadOnlyList<Rock> Rocks => rocks;

        public void Init(GameManager g) { gm = g; }

        ZoneDefinition Zone => gm.Zones.Current;

        public void ResetZone()
        {
            foreach (var r in rocks) if (r != null) Destroy(r.gameObject);
            foreach (var p in pickups) if (p != null) Destroy(p.gameObject);
            rocks.Clear();
            pickups.Clear();
            respawnTimers.Clear();

            int count = (int)gm.Stat(StatType.RockCount);
            for (int i = 0; i < count; i++) SpawnRock(false);

            var z = Zone;
            if (z.isAbyss && gm.Save.abyssDepth % 10 == 0) SpawnBoss("Abyss Warden " + (gm.Save.abyssDepth / 10));
        }

        // ------------------------------------------------------------ Spawning
        float FreeX(float minGap)
        {
            float best = 0; float bestGap = -1;
            for (int attempt = 0; attempt < 24; attempt++)
            {
                float x = UnityEngine.Random.Range(RockMinX, ZoneManager.WorldHalfWidth - 2f);
                float gap = float.MaxValue;
                foreach (var r in rocks) if (r != null) gap = Mathf.Min(gap, Mathf.Abs(r.transform.position.x - x) - r.radius);
                if (gm.Player != null) gap = Mathf.Min(gap, Mathf.Abs(gm.Player.transform.position.x - x) + 0.6f);
                if (gap >= minGap) return x;
                if (gap > bestGap) { bestGap = gap; best = x; }
            }
            return best;
        }

        Rarity RollRarity()
        {
            double luck = Math.Max(1, gm.Stat(StatType.Luck));
            double total = 0;
            var w = new double[5];
            for (int i = 0; i < 5; i++) { w[i] = RarityWeights[i] * Math.Pow(luck, i * 0.6); total += w[i]; }
            double roll = UnityEngine.Random.value * total;
            for (int i = 0; i < 5; i++) { roll -= w[i]; if (roll <= 0) return (Rarity)i; }
            return Rarity.Common;
        }

        void SpawnRock(bool allowBoss)
        {
            var z = Zone;
            bool hasBoss = rocks.Exists(r => r != null && r.isBoss);
            if (allowBoss && !hasBoss && z.index >= 5 && !z.isAbyss && UnityEngine.Random.value < 0.02f)
            {
                SpawnBoss("Ancient Guardian");
                return;
            }
            var rarity = RollRarity();
            double hp = gm.Zones.RockHp(z) * RarityHp[(int)rarity] * UnityEngine.Random.Range(0.85f, 1.15f);
            var rock = Rock.Create(gm, z, rarity, false, FreeX(1.6f), hp);
            rocks.Add(rock);
            if (rarity >= Rarity.Legendary)
            {
                GameEvents.RaiseToast("A " + rarity + " rock appeared!");
                gm.Fx.Sparkle(rock.Center, PetManager.RarityColor(rarity), 10, 1.2f);
            }
        }

        void SpawnBoss(string name)
        {
            var z = Zone;
            double hp = gm.Zones.RockHp(z) * 120;
            var boss = Rock.Create(gm, z, Rarity.Mythic, true, Mathf.Min(ZoneManager.WorldHalfWidth - 4f, 14f), hp, name);
            rocks.Add(boss);
            gm.Audio.Play(Sfx.Boss, 0.8f, 0);
            gm.Fx.Shake(0.4f);
            GameEvents.RaiseToast("A hidden guardian awakens: " + name + "!");
        }

        void Update()
        {
            if (gm == null || !gm.Ready || !gm.Zones.Built) return;
            fullNoticeCooldown -= Time.deltaTime;
            int target = (int)gm.Stat(StatType.RockCount);
            int normal = 0;
            foreach (var r in rocks) if (r != null && !r.isBoss) normal++;
            for (int i = respawnTimers.Count - 1; i >= 0; i--)
            {
                respawnTimers[i] -= Time.deltaTime;
                if (respawnTimers[i] <= 0)
                {
                    respawnTimers.RemoveAt(i);
                    if (normal < target) { SpawnRock(true); normal++; }
                }
            }
            if (normal + respawnTimers.Count < target) respawnTimers.Add(UnityEngine.Random.Range(0.8f, 2.2f));
        }

        // ------------------------------------------------------------ Queries
        public Rock RockAtPoint(Vector2 world)
        {
            Rock best = null; float bestD = float.MaxValue;
            foreach (var r in rocks)
            {
                if (r == null || r.Dead) continue;
                float d = Vector2.Distance(world, r.Center);
                if (d < r.radius + 0.45f && d < bestD) { best = r; bestD = d; }
            }
            return best;
        }

        public Rock NearestRock(float x, float maxDist)
        {
            Rock best = null; float bestD = maxDist;
            foreach (var r in rocks)
            {
                if (r == null || r.Dead) continue;
                float d = Mathf.Abs(r.transform.position.x - x) - r.radius;
                if (d < bestD) { best = r; bestD = d; }
            }
            return best;
        }

        // ------------------------------------------------------------ Hits
        public void Hit(Rock rock, float powerScale = 1f, bool allowSplash = true)
        {
            if (rock == null || rock.Dead) return;
            var z = Zone;
            double power = gm.Stat(StatType.MiningPower) * powerScale;
            double hardness = gm.Zones.Hardness(z, gm.Zones.Depth(z));
            if (power < hardness) power *= 0.25;

            bool crit = UnityEngine.Random.value < gm.Stat(StatType.CritChance);
            double dmg = power * UnityEngine.Random.Range(0.9f, 1.1f) * (crit ? gm.Stat(StatType.CritDamage) : 1);
            if (crit) gm.Save.stats.crits++;

            bool died = rock.Damage(dmg);
            var c = rock.Center;
            gm.Fx.FloatingText(c + Vector3.up * 0.6f, (crit ? "CRIT " : "") + NumberFormatter.Format(dmg),
                crit ? new Color(1f, 0.35f, 0.25f) : Color.white, crit ? 1.25f : 0.85f);
            gm.Fx.Burst(c, z.rockColor, crit ? 9 : 5, crit ? 7f : 5f, 0.14f);
            gm.Audio.Play(crit ? Sfx.Crit : Sfx.Hit, crit ? 0.65f : 0.45f, 0.12f);
            if (crit) gm.Fx.Shake(0.12f);

            if (allowSplash)
            {
                double splash = gm.Stat(StatType.Splash);
                if (splash > 0)
                {
                    foreach (var other in rocks.ToArray())
                    {
                        if (other == null || other == rock || other.Dead) continue;
                        if (Mathf.Abs(other.transform.position.x - rock.transform.position.x) > 3.5f) continue;
                        Hit(other, (float)splash * powerScale, false);
                    }
                }
            }
            if (died) Break(rock);
        }

        void Break(Rock rock)
        {
            var z = Zone;
            var c = rock.Center;
            rocks.Remove(rock);
            int ri = (int)rock.rarity;

            // Ore drops
            double baseCount = UnityEngine.Random.Range(1f, 3.99f);
            double count = baseCount * RarityDrops[ri] * gm.Stat(StatType.OreMultiplier) * (rock.isBoss ? 25 : 1);
            int whole = (int)Math.Floor(count);
            if (UnityEngine.Random.value < count - whole) whole++;
            whole = Math.Max(1, whole);
            var perOre = new double[z.oreIds.Length];
            var weights = OreWeightsByRarity[ri];
            int chunks = Mathf.Min(whole, rock.isBoss ? 12 : 6);
            for (int i = 0; i < whole; i++)
            {
                float roll = UnityEngine.Random.value * 100f;
                int pick = 0;
                for (int k = 0; k < perOre.Length && k < weights.Length; k++) { roll -= weights[k]; if (roll <= 0) { pick = k; break; } pick = k; }
                perOre[pick] += 1;
            }
            for (int k = 0; k < perOre.Length; k++)
            {
                if (perOre[k] <= 0) continue;
                var ore = gm.Db.GetOre(z.oreIds[k]);
                if (ore == null) continue;
                int pieces = Mathf.Clamp(Mathf.CeilToInt(chunks * (float)(perOre[k] / whole)), 1, 6);
                double each = perOre[k] / pieces;
                for (int p = 0; p < pieces; p++)
                {
                    var pickup = OrePickup.Create(gm, ore, each, c);
                    pickups.Add(pickup);
                }
            }
            while (pickups.Count > 90)
            {
                var old = pickups[0];
                pickups.RemoveAt(0);
                if (old != null) Destroy(old.gameObject);
            }

            // Gems
            double gemChance = gm.Stat(StatType.GemChance) * (1 + ri);
            if (rock.isBoss || UnityEngine.Random.value < gemChance)
            {
                double gems = rock.isBoss ? 25 * (1 + z.index) * Math.Max(1, gm.Save.abyssDepth / 5.0) : 1 + ri;
                gems = Math.Round(gems);
                gm.Currency.AddGems(gems);
                gm.Fx.FloatingText(c + Vector3.up * 1.4f, "+" + NumberFormatter.Format(gems) + " Gems", new Color(1f, 0.45f, 0.85f), 1.1f);
                gm.Fx.Sparkle(c, new Color(1f, 0.45f, 0.85f), 6, 0.8f);
            }

            // Boss crystals
            if (rock.isBoss)
            {
                double cr = 1 + Math.Floor(gm.Save.abyssDepth / 10.0);
                gm.Currency.AddCrystals(cr);
                gm.Save.stats.bossesDefeated++;
                gm.Fx.FloatingText(c + Vector3.up * 2.2f, "+" + cr + " Ancient Crystal" + (cr > 1 ? "s" : ""), new Color(0.4f, 1f, 1f), 1.4f);
                gm.Fx.Flash(new Color(0.5f, 1f, 1f, 0.6f));
                gm.Fx.Confetti();
            }

            // XP & stats
            gm.Levels.AddXp(gm.Zones.XpBase(z) * RarityXp[ri] * (rock.isBoss ? 100 : 1));
            gm.Save.stats.rocksBroken++;
            if (rock.rarity == Rarity.Mythic && !rock.isBoss) gm.Save.stats.mythicRocks++;

            // FX
            gm.Fx.Burst(c, z.rockColor, rock.isBoss ? 60 : 18 + ri * 6, 8f, 0.22f);
            if (ri >= 1) gm.Fx.Sparkle(c, PetManager.RarityColor(rock.rarity), 4 + ri * 3, 1f);
            gm.Fx.Shake(rock.isBoss ? 0.6f : 0.08f + ri * 0.05f);
            gm.Audio.Play(Sfx.Break, 0.6f, 0.1f);

            GameEvents.RaiseRockBroken(rock.rarity, rock.isBoss);
            Destroy(rock.gameObject);
        }

        public void RemovePickup(OrePickup p) => pickups.Remove(p);

        public void NotifyBackpackFull()
        {
            if (fullNoticeCooldown > 0) return;
            fullNoticeCooldown = 2.5f;
            gm.Audio.Play(Sfx.Full);
            gm.Fx.FloatingText(gm.Player.transform.position + Vector3.up * 3f, "BACKPACK FULL!", new Color(1f, 0.4f, 0.3f), 1.1f);
            gm.UI.PulseSellButton();
        }

        public int PickupsWaiting => pickups.Count;
    }
}
