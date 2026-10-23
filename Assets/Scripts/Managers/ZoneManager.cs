using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleMiningEmpire
{
    /// <summary>
    /// Vertically descending mining zones: unlocking, travelling by elevator, infinite Abyss depths,
    /// and building each zone's environment (sky, parallax cave layers, ground, buildings, ambience).
    /// </summary>
    public class ZoneManager : MonoBehaviour
    {
        public const float WorldHalfWidth = 22f;
        public const float ElevatorX = -20f;
        public const float StationX = -15.2f;
        public const float SpawnX = -11.5f;

        class Layer { public Transform t; public float factor; public bool ceiling; }

        GameManager gm;
        Transform worldRoot;
        SpriteRenderer sky;
        readonly List<Layer> layers = new List<Layer>();
        readonly List<Transform> clouds = new List<Transform>();
        float ambientTimer;

        public bool Built { get; private set; }
        public ZoneDefinition Current => gm.Db.zones[Mathf.Clamp(gm.Save.currentZone, 0, gm.Db.zones.Count - 1)];
        public ZoneDefinition Abyss => gm.Db.zones.Find(z => z.isAbyss);
        public bool AtStation => gm.Save.currentZone == 0 && gm.Player != null &&
                                 Mathf.Abs(gm.Player.transform.position.x - StationX) < 3.2f;

        public void Init(GameManager g) { gm = g; }

        // ------------------------------------------------------------ Numbers
        public double ValueFactor(int zoneIndex)
        {
            var g = GameManager.Instance;
            var zones = g.Db.zones;
            var z = zones[Mathf.Clamp(zoneIndex, 0, zones.Count - 1)];
            double sum = 0; int n = 0;
            foreach (var id in z.oreIds)
            {
                var o = g.Db.GetOre(id);
                if (o == null) continue;
                sum += o.baseValue; n++;
            }
            double v = n > 0 ? sum / n : 1;
            if (z.isAbyss) v *= Math.Pow(1.45, Math.Max(0, g.Save.abyssUnlockedDepth - 1));
            return v;
        }

        public int Depth(ZoneDefinition z) => z.isAbyss ? gm.Save.abyssDepth : 1;
        public double RockHp(ZoneDefinition z) => z.baseRockHp * (z.isAbyss ? Math.Pow(1.5, Depth(z) - 1) : 1);
        public double Hardness(ZoneDefinition z, int depth) => z.hardness * (z.isAbyss ? Math.Pow(1.45, depth - 1) : 1);
        public double XpBase(ZoneDefinition z) => z.xpBase * (z.isAbyss ? Math.Pow(1.25, Depth(z) - 1) : 1);

        public string DisplayName(ZoneDefinition z) => z.isAbyss ? z.displayName + " - Depth " + gm.Save.abyssDepth : z.displayName;

        // ------------------------------------------------------------ Unlocks
        public bool IsUnlocked(int i) => i <= gm.Save.unlockedZone;

        /// <summary>Null if the zone can be unlocked now, otherwise the reason it can't.</summary>
        public string UnlockBlocker(int i)
        {
            if (i < 0 || i >= gm.Db.zones.Count) return "Unknown zone";
            if (IsUnlocked(i)) return null;
            var z = gm.Db.zones[i];
            if (i != gm.Save.unlockedZone + 1) return "Unlock the previous zone first";
            if (gm.Save.stats.prestiges < z.requiresPrestiges) return "Requires " + z.requiresPrestiges + " Rebirth";
            if (gm.Stat(StatType.MiningPower) < z.hardness) return "Needs " + NumberFormatter.Format(z.hardness) + " Mining Power";
            if (!gm.Currency.CanAfford(CurrencyType.Coins, z.unlockCost)) return "Costs " + NumberFormatter.Format(z.unlockCost) + " coins";
            return null;
        }

        public bool TryUnlock(int i)
        {
            if (IsUnlocked(i) || UnlockBlocker(i) != null) { gm.Audio.Play(Sfx.Error); return false; }
            var z = gm.Db.zones[i];
            gm.Currency.TrySpend(CurrencyType.Coins, z.unlockCost);
            gm.Save.unlockedZone = i;
            if (i > gm.Save.stats.deepestZoneEver) gm.Save.stats.deepestZoneEver = i;
            if (z.isAbyss)
            {
                gm.Save.abyssUnlockedDepth = Math.Max(1, gm.Save.abyssUnlockedDepth);
                gm.Save.abyssDepth = gm.Save.abyssUnlockedDepth;
                gm.Save.stats.maxAbyssDepth = Math.Max(gm.Save.stats.maxAbyssDepth, gm.Save.abyssDepth);
            }
            gm.Audio.Play(Sfx.Unlock);
            GameEvents.RaiseZoneUnlocked(i);
            GameEvents.RaiseToast("Unlocked: " + z.displayName + "!");
            EnterZone(i, false);
            return true;
        }

        public double DescendCost => Abyss != null ? Abyss.unlockCost * Math.Pow(3, gm.Save.abyssUnlockedDepth) : double.MaxValue;
        public double NextDepthHardness => Abyss != null ? Hardness(Abyss, gm.Save.abyssUnlockedDepth + 1) : double.MaxValue;

        public string DescendBlocker()
        {
            var a = Abyss;
            if (a == null || !IsUnlocked(a.index)) return "Unlock the Abyss first";
            if (gm.Stat(StatType.MiningPower) < NextDepthHardness) return "Needs " + NumberFormatter.Format(NextDepthHardness) + " Mining Power";
            if (!gm.Currency.CanAfford(CurrencyType.Coins, DescendCost)) return "Costs " + NumberFormatter.Format(DescendCost) + " coins";
            return null;
        }

        public bool TryDescend()
        {
            if (DescendBlocker() != null) { gm.Audio.Play(Sfx.Error); return false; }
            gm.Currency.TrySpend(CurrencyType.Coins, DescendCost);
            gm.Save.abyssUnlockedDepth++;
            gm.Save.abyssDepth = gm.Save.abyssUnlockedDepth;
            gm.Save.stats.maxAbyssDepth = Math.Max(gm.Save.stats.maxAbyssDepth, gm.Save.abyssDepth);
            gm.Audio.Play(Sfx.Unlock);
            GameEvents.RaiseToast("Descended to Abyss depth " + gm.Save.abyssDepth + (gm.Save.abyssDepth % 10 == 0 ? " - a Warden awaits!" : ""));
            GameEvents.RaiseStatsChanged();
            EnterZone(Abyss.index, false);
            return true;
        }

        // ------------------------------------------------------------ Travel
        public void EnterZone(int index, bool instant)
        {
            index = Mathf.Clamp(index, 0, Mathf.Min(gm.Save.unlockedZone, gm.Db.zones.Count - 1));
            bool restoring = instant && index == gm.Save.currentZone;
            gm.Save.currentZone = index;
            var z = Current;
            if (z.isAbyss) gm.Save.abyssDepth = gm.Save.abyssUnlockedDepth;
            if (index > gm.Save.stats.deepestZoneEver) gm.Save.stats.deepestZoneEver = index;

            BuildWorld(z);
            float px = restoring && Mathf.Abs(gm.Save.playerX) > 0.01f ? Mathf.Clamp(gm.Save.playerX, -WorldHalfWidth + 1, WorldHalfWidth - 1) : SpawnX;
            gm.Player.Teleport(new Vector3(px, 0, 0));
            gm.Mining.ResetZone();
            gm.Cam.Snap();
            gm.Audio.PlayZoneMusic(z);
            if (!instant)
            {
                gm.Fx.Flash(new Color(0, 0, 0, 0.85f));
                gm.Audio.Play(Sfx.Dash);
            }
            GameEvents.RaiseZoneEntered(index);
        }

        // ------------------------------------------------------------ World building
        void BuildWorld(ZoneDefinition z)
        {
            if (worldRoot != null) Destroy(worldRoot.gameObject);
            layers.Clear();
            clouds.Clear();
            worldRoot = new GameObject("World - " + z.displayName).transform;

            gm.Cam.Camera.backgroundColor = z.skyBottom;

            // Sky / cave darkness gradient (follows camera)
            sky = MakeSprite("Sky", SpriteFactory.VerticalGradient(z.skyTop, z.skyBottom), -100);

            bool surface = z.index == 0;
            if (surface)
            {
                var sun = MakeSprite("Sun", SpriteFactory.SoftGlow, -99);
                sun.color = new Color(1f, 0.95f, 0.6f, 0.9f);
                sun.transform.localScale = Vector3.one * 5f;
                AddLayer(sun.transform, 0.92f, false, new Vector3(8, 9.5f, 0));
                for (int i = 0; i < 5; i++)
                {
                    var c = MakeSprite("Cloud", SpriteFactory.Cloud(i * 7 + 3), -97);
                    c.transform.position = new Vector3(-30 + i * 14 + UnityEngine.Random.Range(-3f, 3f), 7.5f + UnityEngine.Random.Range(0f, 3f), 0);
                    c.transform.localScale = Vector3.one * UnityEngine.Random.Range(1.2f, 2f);
                    clouds.Add(c.transform);
                }
                AddTiledLayer(SpriteFactory.Silhouette(0, 11, Color.Lerp(z.wallColor, z.skyBottom, 0.55f)), -95, 0.75f, new Vector3(0, -0.2f, 0), 1.6f);
                AddTiledLayer(SpriteFactory.Silhouette(0, 23, Color.Lerp(z.wallColor, z.skyBottom, 0.2f)), -90, 0.5f, new Vector3(0, -0.4f, 0), 1.2f);
            }
            else
            {
                int kind = z.index == 5 ? 3 : 1;
                AddTiledLayer(SpriteFactory.Silhouette(kind, 100 + z.index, Color.Lerp(z.wallColor, z.skyBottom, 0.45f)), -95, 0.8f, new Vector3(0, 0f, 0), 1.6f);
                AddTiledLayer(SpriteFactory.Silhouette(1, 200 + z.index, z.wallColor), -90, 0.55f, new Vector3(0, -0.3f, 0), 1.15f);
                AddTiledLayer(SpriteFactory.Silhouette(2, 300 + z.index, Color.Lerp(z.wallColor, Color.black, 0.25f)), -88, 0.65f, Vector3.zero, 1.3f, true);
            }

            // Ground
            var ground = MakeSprite("Ground", SpriteFactory.Ground(z.groundColor, z.groundTop, z.index), 0);
            ground.drawMode = SpriteDrawMode.Tiled;
            ground.size = new Vector2(WorldHalfWidth * 2 + 60, 14);
            ground.transform.position = new Vector3(0, 0.05f, 0);

            // Edge walls
            for (int s = -1; s <= 1; s += 2)
            {
                var wall = MakeSprite("Edge", SpriteFactory.White, -30);
                wall.color = Color.Lerp(z.wallColor, Color.black, 0.35f);
                wall.transform.position = new Vector3(s * (WorldHalfWidth + 6.5f), 10, 0);
                wall.transform.localScale = new Vector3(12, 30, 1);
            }

            // Elevator (every zone)
            var elev = MakeSprite("Elevator", SpriteFactory.Elevator, -20);
            elev.transform.position = new Vector3(ElevatorX, 0, 0);
            MakeLabel("ELEVATOR", new Vector3(ElevatorX, 7.6f, 0), new Color(1f, 0.9f, 0.4f), 0.7f);

            if (surface)
            {
                var st = MakeSprite("Trading Station", SpriteFactory.TradingStation, -20);
                st.transform.position = new Vector3(StationX, 0, 0);
                MakeLabel("TRADING POST", new Vector3(StationX, 5.0f, 0), new Color(1f, 0.85f, 0.3f), 0.8f);
                MakeLabel("Sell here for 100%", new Vector3(StationX, 4.4f, 0), Color.white, 0.5f);
            }

            Built = true;
            LateUpdate();
        }

        SpriteRenderer MakeSprite(string name, Sprite s, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(worldRoot, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sortingOrder = order;
            return sr;
        }

        void MakeLabel(string text, Vector3 pos, Color c, float scale)
        {
            var go = new GameObject("Label " + text);
            go.transform.SetParent(worldRoot, false);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * scale;
            var tm = go.AddComponent<TextMesh>();
            tm.font = UIFactory.DefaultFont;
            tm.GetComponent<MeshRenderer>().sharedMaterial = tm.font.material;
            tm.GetComponent<MeshRenderer>().sortingOrder = -15;
            tm.text = text;
            tm.fontSize = 64;
            tm.characterSize = 0.09f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.fontStyle = FontStyle.Bold;
            tm.color = c;
        }

        void AddLayer(Transform t, float factor, bool ceiling, Vector3 basePos)
        {
            t.position = basePos;
            layers.Add(new Layer { t = t, factor = factor, ceiling = ceiling });
        }

        void AddTiledLayer(Sprite s, int order, float factor, Vector3 basePos, float scale, bool ceiling = false)
        {
            var sr = MakeSprite("Parallax", s, order);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(160, s.rect.height / s.pixelsPerUnit);
            sr.transform.localScale = Vector3.one * scale;
            AddLayer(sr.transform, factor, ceiling, basePos);
        }

        void LateUpdate()
        {
            if (!Built || gm.Cam == null) return;
            var cam = gm.Cam.Camera;
            Vector3 cp = cam.transform.position;
            float h = cam.orthographicSize * 2f, w = h * cam.aspect;

            if (sky != null)
            {
                sky.transform.position = new Vector3(cp.x, cp.y, 0);
                var size = sky.sprite.bounds.size;
                sky.transform.localScale = new Vector3((w + 2f) / size.x, (h + 2f) / size.y, 1);
            }

            foreach (var l in layers)
            {
                if (l.t == null) continue;
                var p = l.t.position;
                p.x = cp.x * l.factor;
                if (l.ceiling) p.y = cp.y + cam.orthographicSize + 0.2f;
                else if (l.factor > 0.9f) p.y = cp.y + cam.orthographicSize * 0.55f;
                l.t.position = p;
            }

            foreach (var c in clouds)
            {
                if (c == null) continue;
                var p = c.position;
                p.x += Time.deltaTime * 0.35f;
                if (p.x > WorldHalfWidth + 18) p.x = -WorldHalfWidth - 18;
                c.position = p;
            }

            // ambient motes
            if (Current.index > 0)
            {
                ambientTimer -= Time.deltaTime;
                if (ambientTimer <= 0)
                {
                    ambientTimer = gm.Save.lowFx ? 0.9f : 0.3f;
                    var pos = new Vector3(cp.x + UnityEngine.Random.Range(-w / 2, w / 2), UnityEngine.Random.Range(0.5f, cp.y + cam.orthographicSize - 1), 0);
                    gm.Fx.Sparkle(pos, Current.ambientColor, 1, 0.1f);
                }
            }
        }
    }
}
