using System.Collections.Generic;
using UnityEngine;

namespace IdleMiningEmpire
{
    /// <summary>
    /// Procedurally paints every sprite in the game (rocks, miner, pets, buildings, icons, UI skins)
    /// so the project ships with zero imported art and a tiny WebGL download.
    /// </summary>
    public static class SpriteFactory
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        static Sprite Cached(string key, System.Func<Sprite> make)
        {
            if (cache.TryGetValue(key, out var s) && s != null) return s;
            s = make();
            s.name = key;
            cache[key] = s;
            return s;
        }

        static Color Shade(Color c, float f) => f >= 0 ? Color.Lerp(c, Color.white, f) : Color.Lerp(c, Color.black, -f);
        static Color A(Color c, float a) { c.a = a; return c; }

        // ------------------------------------------------------------ PRIMITIVES
        public static Sprite White => Cached("white", () =>
        {
            var p = new PixelCanvas(4, 4);
            p.Rect(0, 0, 4, 4, Color.white);
            return p.ToSprite(4, new Vector2(0.5f, 0.5f));
        });

        public static Sprite Circle => Cached("circle", () =>
        {
            var p = new PixelCanvas(64, 64);
            p.Circle(32, 32, 31, Color.white);
            return p.ToSprite(64, new Vector2(0.5f, 0.5f), FilterMode.Bilinear);
        });

        public static Sprite SoftGlow => Cached("glow", () =>
        {
            var p = new PixelCanvas(64, 64);
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(32, 32)) / 32f;
                    float a = Mathf.Clamp01(1 - d);
                    p.Set(x, y, new Color(1, 1, 1, a * a));
                }
            return p.ToSprite(64, new Vector2(0.5f, 0.5f), FilterMode.Bilinear);
        });

        /// <summary>9-sliced rounded rectangle for UI panels and buttons.</summary>
        public static Sprite RoundedRect => Cached("rounded", () =>
        {
            const int S = 48, R = 14;
            var p = new PixelCanvas(S, S);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, R, S - R), cy = Mathf.Clamp(y + 0.5f, R, S - R);
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    float a = Mathf.Clamp01(R - d + 0.5f);
                    p.Set(x, y, new Color(1, 1, 1, a));
                }
            return p.ToSprite(100, new Vector2(0.5f, 0.5f), FilterMode.Bilinear, TextureWrapMode.Clamp, new Vector4(R + 2, R + 2, R + 2, R + 2));
        });

        /// <summary>Rounded rect outline (for highlights / borders).</summary>
        public static Sprite RoundedFrame => Cached("roundedFrame", () =>
        {
            const int S = 48, R = 14;
            var p = new PixelCanvas(S, S);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, R, S - R), cy = Mathf.Clamp(y + 0.5f, R, S - R);
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    float outer = Mathf.Clamp01(R - d + 0.5f), inner = Mathf.Clamp01(R - 3 - d + 0.5f);
                    p.Set(x, y, new Color(1, 1, 1, outer - inner));
                }
            return p.ToSprite(100, new Vector2(0.5f, 0.5f), FilterMode.Bilinear, TextureWrapMode.Clamp, new Vector4(R + 2, R + 2, R + 2, R + 2));
        });

        public static Sprite VerticalGradient(Color top, Color bottom)
        {
            string key = "grad_" + ColorUtility.ToHtmlStringRGBA(top) + ColorUtility.ToHtmlStringRGBA(bottom);
            return Cached(key, () =>
            {
                var p = new PixelCanvas(4, 128);
                for (int y = 0; y < 128; y++)
                {
                    float t = y / 127f;
                    var c = Color.Lerp(bottom, top, t * t * (3 - 2 * t));
                    for (int x = 0; x < 4; x++) p.Set(x, y, c);
                }
                return p.ToSprite(4, new Vector2(0.5f, 0.5f), FilterMode.Bilinear);
            });
        }

        // ------------------------------------------------------------ BACKGROUND
        /// <summary>Repeating silhouette band. kind 0 = rolling hills, 1 = jagged cave rocks, 2 = ceiling stalactites, 3 = ruins pillars.</summary>
        public static Sprite Silhouette(int kind, int seed, Color c)
        {
            string key = "sil_" + kind + "_" + seed + "_" + ColorUtility.ToHtmlStringRGBA(c);
            return Cached(key, () =>
            {
                const int W = 256, H = 96;
                var p = new PixelCanvas(W, H);
                var r = new System.Random(seed);
                float ph1 = (float)r.NextDouble() * 6.28f, ph2 = (float)r.NextDouble() * 6.28f, ph3 = (float)r.NextDouble() * 6.28f;
                for (int x = 0; x < W; x++)
                {
                    float u = x / (float)W * Mathf.PI * 2f;
                    float h;
                    switch (kind)
                    {
                        case 0: h = 40 + 14 * Mathf.Sin(u * 2 + ph1) + 8 * Mathf.Sin(u * 5 + ph2) + 3 * Mathf.Sin(u * 11 + ph3); break;
                        case 3:
                            h = 30 + 6 * Mathf.Sin(u * 3 + ph1);
                            int m = x % 64;
                            if (m > 20 && m < 34) h = 78 + ((x / 64) % 2) * 8;
                            if (m >= 18 && m <= 36 && h > 70 && (m == 18 || m == 36)) h -= 4;
                            break;
                        default:
                            float jag = Mathf.Abs(Mathf.Sin(u * 7 + ph1)) * 22 + Mathf.Abs(Mathf.Sin(u * 13 + ph2)) * 12;
                            h = 22 + jag + 6 * Mathf.Sin(u * 2 + ph3);
                            break;
                    }
                    int hi = Mathf.Clamp(Mathf.RoundToInt(h), 1, H);
                    for (int y = 0; y < hi; y++)
                    {
                        float shade = 1f - 0.18f * (y / (float)hi);
                        var col = new Color(c.r * shade, c.g * shade, c.b * shade, c.a);
                        if (kind == 2) p.Set(x, H - 1 - y, col); else p.Set(x, y, col);
                    }
                }
                return p.ToSprite(16, new Vector2(0.5f, kind == 2 ? 1f : 0f), FilterMode.Point, TextureWrapMode.Repeat);
            });
        }

        public static Sprite Cloud(int seed)
        {
            return Cached("cloud_" + seed, () =>
            {
                var p = new PixelCanvas(64, 28);
                var r = new System.Random(seed);
                for (int i = 0; i < 6; i++)
                {
                    float cx = 10 + (float)r.NextDouble() * 44, cy = 10 + (float)r.NextDouble() * 6, rad = 6 + (float)r.NextDouble() * 6;
                    p.Circle(cx, cy, rad, new Color(1, 1, 1, 0.9f));
                }
                return p.ToSprite(16, new Vector2(0.5f, 0.5f));
            });
        }

        /// <summary>Repeating ground tile: soil with a coloured top strip.</summary>
        public static Sprite Ground(Color soil, Color top, int seed)
        {
            string key = "ground_" + ColorUtility.ToHtmlStringRGB(soil) + ColorUtility.ToHtmlStringRGB(top) + seed;
            return Cached(key, () =>
            {
                const int S = 32;
                var p = new PixelCanvas(S, S);
                var r = new System.Random(seed);
                for (int y = 0; y < S; y++)
                    for (int x = 0; x < S; x++)
                    {
                        float n = (float)r.NextDouble();
                        var c = Shade(soil, (n - 0.5f) * 0.18f);
                        p.Set(x, y, c);
                    }
                for (int i = 0; i < 10; i++)
                {
                    int x = r.Next(S), y = r.Next(S - 8);
                    p.Rect(x, y, 2 + r.Next(2), 2, Shade(soil, -0.25f));
                    p.Set(x, y + 2, Shade(soil, 0.2f));
                }
                for (int x = 0; x < S; x++)
                {
                    int h = 4 + (int)(2 * Mathf.Sin(x * 0.7f + seed)) + r.Next(2);
                    for (int y = S - h; y < S; y++) p.Set(x, y, Shade(top, (y == S - 1) ? 0.25f : 0));
                    p.Set(x, S - h - 1, Shade(top, -0.3f));
                }
                return p.ToSprite(16, new Vector2(0.5f, 1f), FilterMode.Point, TextureWrapMode.Repeat);
            });
        }

        // ----------------------------------------------------------------- ROCKS
        public static Sprite Rock(ZoneDefinition zone, Color[] oreColors, Rarity rarity, int variant, bool boss = false)
        {
            string key = "rock_" + zone.id + "_" + (int)rarity + "_" + variant + (boss ? "_b" : "");
            return Cached(key, () =>
            {
                int S = boss ? 96 : 64;
                var p = new PixelCanvas(S, S);
                var r = new System.Random(zone.index * 1000 + variant * 37 + (int)rarity * 7 + (boss ? 999 : 0));
                float cx = S / 2f, cy = S * 0.44f, baseR = S * 0.40f;
                float[] amp = new float[6], ph = new float[6];
                for (int i = 0; i < 6; i++) { amp[i] = (float)r.NextDouble() * (0.12f / (i + 1)); ph[i] = (float)r.NextDouble() * 6.28f; }
                Color baseCol = zone.rockColor;
                if (rarity == Rarity.Mythic) baseCol = Color.Lerp(baseCol, new Color(0.35f, 0.1f, 0.45f), 0.5f);
                if (boss) baseCol = Color.Lerp(baseCol, new Color(0.15f, 0.2f, 0.25f), 0.4f);

                for (int y = 0; y < S; y++)
                    for (int x = 0; x < S; x++)
                    {
                        float dx = x + 0.5f - cx, dy = (y + 0.5f - cy) * 1.15f;
                        float ang = Mathf.Atan2(dy, dx);
                        float rr = baseR;
                        for (int i = 0; i < 6; i++) rr *= 1 + amp[i] * Mathf.Sin(ang * (i + 2) + ph[i]);
                        if (dy < -baseR * 0.55f) continue; // flat-ish bottom
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        if (d > rr) continue;
                        // lighting: top-left bright, bottom-right dark, facets
                        float light = (-dx * 0.5f + dy) / baseR;
                        float facet = Mathf.Floor((ang + 3.2f) * 2.2f) % 2 == 0 ? 0.05f : -0.03f;
                        float edge = d / rr;
                        var c = Shade(baseCol, Mathf.Clamp(light * 0.25f + facet - edge * edge * 0.18f, -0.5f, 0.4f));
                        p.Set(x, y, c);
                    }

                // cracks
                for (int k = 0; k < 2 + variant; k++)
                {
                    float a0 = (float)r.NextDouble() * 6.28f;
                    float x0 = cx + Mathf.Cos(a0) * baseR * 0.2f, y0 = cy + Mathf.Sin(a0) * baseR * 0.2f;
                    for (int seg = 0; seg < 3; seg++)
                    {
                        float a = a0 + ((float)r.NextDouble() - 0.5f) * 1.2f;
                        float len = baseR * (0.18f + (float)r.NextDouble() * 0.2f);
                        float x1 = x0 + Mathf.Cos(a) * len, y1 = y0 + Mathf.Sin(a) * len;
                        LineIfOpaque(p, x0, y0, x1, y1, Shade(baseCol, -0.4f));
                        x0 = x1; y0 = y1;
                    }
                }

                // ore specks
                int specks = new[] { 5, 8, 11, 15, 20 }[(int)rarity] + (boss ? 15 : 0);
                for (int k = 0; k < specks; k++)
                {
                    var oc = oreColors[Mathf.Min(oreColors.Length - 1, r.Next(Mathf.Min(oreColors.Length, 1 + (int)rarity)))];
                    int sx = r.Next(S), sy = r.Next(S);
                    if (p.Alpha(sx, sy) < 1 || p.Alpha(sx + 2, sy + 2) < 1 || p.Alpha(sx - 1, sy - 1) < 1) continue;
                    int sz = 2 + r.Next(2) + (rarity >= Rarity.Epic ? 1 : 0);
                    p.Rect(sx, sy, sz, sz, oc);
                    p.Set(sx, sy + sz - 1, Shade(oc, 0.6f));
                    p.Set(sx + sz - 1, sy, Shade(oc, -0.35f));
                }

                // mythic/legendary crystals on top
                if (rarity >= Rarity.Legendary || boss)
                {
                    var cc = rarity == Rarity.Mythic ? new Color(1f, 0.4f, 0.9f) : new Color(1f, 0.75f, 0.2f);
                    if (boss) cc = new Color(0.3f, 1f, 0.95f);
                    for (int k = 0; k < 3; k++)
                    {
                        float bx = cx + (k - 1) * baseR * 0.45f, by = cy + baseR * 0.55f + (k == 1 ? 3 : 0);
                        float hgt = baseR * (k == 1 ? 0.75f : 0.5f);
                        p.Triangle(new Vector2(bx - 4, by - 4), new Vector2(bx + 4, by - 4), new Vector2(bx, by + hgt), cc);
                        p.Line(bx - 1, by - 2, bx - 1, by + hgt * 0.6f, Shade(cc, 0.6f));
                    }
                }

                if (boss)
                {
                    // glowing eyes
                    var eye = new Color(0.4f, 1f, 1f);
                    p.Rect((int)(cx - 14), (int)cy + 2, 8, 5, eye);
                    p.Rect((int)(cx + 6), (int)cy + 2, 8, 5, eye);
                    p.Rect((int)(cx - 12), (int)cy + 3, 3, 3, Color.white);
                    p.Rect((int)(cx + 8), (int)cy + 3, 3, 3, Color.white);
                    p.Line(cx - 10, cy - 10, cx + 10, cy - 10, Shade(baseCol, -0.6f), 2);
                }

                p.Outline(Shade(baseCol, -0.65f));
                return p.ToSprite(32, new Vector2(0.5f, 0.12f));
            });
        }

        static void LineIfOpaque(PixelCanvas p, float x0, float y0, float x1, float y1, Color c)
        {
            int steps = Mathf.CeilToInt(Vector2.Distance(new Vector2(x0, y0), new Vector2(x1, y1)) * 1.5f) + 1;
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                int x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t)), y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));
                if (p.Alpha(x, y) > 0.9f) p.Set(x, y, c);
            }
        }

        // ---------------------------------------------------------------- ORE GEM
        public static Sprite OreChunk(OreDefinition ore)
        {
            return Cached("ore_" + ore.id, () =>
            {
                var p = new PixelCanvas(12, 12);
                p.Triangle(new Vector2(1, 5), new Vector2(6, 11), new Vector2(11, 5), ore.color);
                p.Triangle(new Vector2(1, 5), new Vector2(6, 0), new Vector2(11, 5), Shade(ore.color, -0.25f));
                p.Triangle(new Vector2(3, 6), new Vector2(6, 10), new Vector2(6, 6), ore.highlight);
                p.Outline(Shade(ore.color, -0.6f));
                return p.ToSprite(24, new Vector2(0.5f, 0.5f));
            });
        }

        // ---------------------------------------------------------------- MINER
        /// <summary>Miner body. frame 0/1 = walk frames. style 0 = human, 1 = robot, 2 = AI drone.</summary>
        public static Sprite Miner(Color helmet, Color suit, int frame, int style = 0)
        {
            string key = "miner_" + ColorUtility.ToHtmlStringRGB(helmet) + ColorUtility.ToHtmlStringRGB(suit) + frame + "_" + style;
            return Cached(key, () =>
            {
                var p = new PixelCanvas(24, 32);
                var skin = new Color(1f, 0.82f, 0.66f);
                var dark = Shade(suit, -0.45f);
                var boots = new Color(0.25f, 0.18f, 0.12f);
                if (style == 2)
                {
                    // floating AI drone
                    p.Ellipse(12, 16, 9, 7, suit);
                    p.Ellipse(12, 18, 7, 4, Shade(suit, 0.3f));
                    p.Rect(7, 15, 10, 3, new Color(0.1f, 0.1f, 0.15f));
                    p.Rect(9 + frame * 3, 16, 3, 1, new Color(0.3f, 1f, 1f));
                    p.Line(12, 23, 12, 27, Shade(suit, -0.3f));
                    p.Circle(12, 28, 1.5f, helmet);
                    p.Rect(5, 7, 14, 2, A(helmet, 0.6f));
                    p.Outline(new Color(0.08f, 0.08f, 0.1f));
                    return p.ToSprite(16, new Vector2(0.5f, 0f));
                }
                // legs
                int l1 = frame == 0 ? 0 : 1, l2 = frame == 0 ? 0 : -1;
                p.Rect(7 + l1, 2, 4, 7, dark);
                p.Rect(13 + l2, 2, 4, 7, dark);
                p.Rect(6 + l1, 0, 5, 3, boots);
                p.Rect(13 + l2, 0, 5, 3, boots);
                // body
                p.Rect(6, 9, 12, 10, suit);
                p.Rect(6, 11, 12, 2, Shade(suit, 0.2f));
                p.Rect(6, 9, 12, 2, new Color(0.35f, 0.22f, 0.1f)); // belt
                p.Rect(11, 9, 2, 2, new Color(1f, 0.85f, 0.3f));
                // arm (front)
                p.Rect(16, 11, 3, 7, Shade(suit, -0.15f));
                if (style == 1)
                {
                    // robot head
                    p.Rect(7, 19, 10, 9, new Color(0.7f, 0.74f, 0.8f));
                    p.Rect(8, 22, 8, 3, new Color(0.1f, 0.12f, 0.15f));
                    p.Rect(9 + frame * 3, 23, 2, 1, new Color(1f, 0.3f, 0.3f));
                    p.Line(12, 28, 12, 30, new Color(0.5f, 0.5f, 0.55f));
                    p.Circle(12, 30.5f, 1.2f, helmet);
                }
                else
                {
                    // head
                    p.Rect(8, 19, 9, 7, skin);
                    p.Rect(14, 22, 2, 2, new Color(0.15f, 0.1f, 0.1f)); // eye
                    p.Rect(9, 19, 7, 1, Shade(skin, -0.15f));
                    // helmet
                    p.Rect(7, 25, 11, 4, helmet);
                    p.Rect(6, 25, 13, 1, Shade(helmet, -0.3f));
                    p.Rect(8, 28, 9, 1, Shade(helmet, 0.3f));
                    p.Rect(16, 26, 3, 2, new Color(1f, 1f, 0.7f)); // lamp
                }
                p.Outline(new Color(0.1f, 0.07f, 0.06f));
                return p.ToSprite(16, new Vector2(0.5f, 0f));
            });
        }

        public static Sprite Pickaxe(Color head)
        {
            return Cached("pick_" + ColorUtility.ToHtmlStringRGB(head), () =>
            {
                var p = new PixelCanvas(24, 24);
                var wood = new Color(0.55f, 0.35f, 0.18f);
                p.Line(3, 3, 15, 15, wood, 2);
                p.Line(4, 3, 15, 14, Shade(wood, 0.2f));
                // curved head
                for (int i = -8; i <= 8; i++)
                {
                    float t = i / 8f;
                    float x = 15 + i * 0.85f + 0.5f, y = 15 - i * 0.85f + (1 - t * t) * 3.5f;
                    p.Rect(Mathf.RoundToInt(x) - 1, Mathf.RoundToInt(y) - 1, 3, 3, head);
                    p.Set(Mathf.RoundToInt(x), Mathf.RoundToInt(y) + 1, Shade(head, 0.45f));
                }
                p.Outline(new Color(0.1f, 0.08f, 0.06f));
                return p.ToSprite(16, new Vector2(3f / 24f, 3f / 24f));
            });
        }

        // ------------------------------------------------------------ BUILDINGS
        public static Sprite TradingStation => Cached("station", () =>
        {
            var p = new PixelCanvas(80, 72);
            var wood = new Color(0.6f, 0.38f, 0.2f);
            p.Rect(6, 0, 68, 40, wood);
            for (int x = 6; x < 74; x += 8) p.Rect(x, 0, 1, 40, Shade(wood, -0.3f));
            p.Rect(4, 38, 72, 4, Shade(wood, -0.35f));
            // awning stripes
            for (int i = 0; i < 9; i++)
                p.Triangle(new Vector2(2 + i * 8.5f, 42), new Vector2(10.5f + i * 8.5f, 42), new Vector2(6.25f + i * 8.5f, 34),
                    i % 2 == 0 ? new Color(0.9f, 0.2f, 0.2f) : Color.white);
            p.Rect(2, 42, 76, 8, new Color(0.9f, 0.2f, 0.2f));
            for (int x = 2; x < 78; x += 12) p.Rect(x, 42, 6, 8, Color.white);
            // counter + window
            p.Rect(14, 14, 52, 18, new Color(0.15f, 0.1f, 0.08f));
            p.Rect(10, 12, 60, 4, Shade(wood, 0.25f));
            // sign
            p.Rect(22, 52, 36, 18, new Color(0.25f, 0.18f, 0.1f));
            p.Rect(24, 54, 32, 14, new Color(1f, 0.82f, 0.25f));
            p.Circle(40, 61, 5, new Color(0.85f, 0.6f, 0.1f));
            p.Rect(39, 57, 2, 8, new Color(1f, 0.95f, 0.6f));
            // ore crates
            p.Rect(18, 18, 10, 8, new Color(0.45f, 0.3f, 0.15f));
            p.Rect(19, 23, 3, 3, new Color(1f, 0.8f, 0.2f));
            p.Rect(23, 22, 3, 3, new Color(0.3f, 0.9f, 0.5f));
            p.Outline(new Color(0.12f, 0.08f, 0.05f));
            return p.ToSprite(16, new Vector2(0.5f, 0f));
        });

        public static Sprite Elevator => Cached("elevator", () =>
        {
            var p = new PixelCanvas(36, 112);
            var metal = new Color(0.45f, 0.48f, 0.55f);
            p.Rect(0, 0, 4, 112, metal);
            p.Rect(32, 0, 4, 112, metal);
            for (int y = 0; y < 112; y += 12) p.Line(4, y, 32, y + 12, Shade(metal, -0.25f));
            p.Rect(16, 34, 2, 78, new Color(0.2f, 0.2f, 0.2f)); // cable
            // cage
            p.Rect(4, 0, 28, 34, new Color(0.2f, 0.22f, 0.28f));
            for (int x = 6; x < 32; x += 5) p.Rect(x, 2, 1, 30, Shade(metal, 0.2f));
            p.Rect(4, 32, 28, 3, Shade(metal, 0.3f));
            p.Rect(13, 14, 10, 8, new Color(0.95f, 0.85f, 0.2f));
            p.Triangle(new Vector2(15, 16), new Vector2(21, 16), new Vector2(18, 20), new Color(0.2f, 0.15f, 0.05f));
            p.Outline(new Color(0.08f, 0.08f, 0.1f));
            return p.ToSprite(16, new Vector2(0.5f, 0f));
        });

        public static Sprite Drill(Color body, int seed)
        {
            return Cached("drill_" + ColorUtility.ToHtmlStringRGB(body) + seed, () =>
            {
                var p = new PixelCanvas(48, 56);
                var steel = new Color(0.6f, 0.62f, 0.68f);
                // tracks
                p.Rect(4, 10, 40, 8, new Color(0.2f, 0.2f, 0.22f));
                for (int x = 6; x < 44; x += 6) p.Circle(x + 1, 14, 3, new Color(0.45f, 0.45f, 0.5f));
                // body
                p.Rect(8, 18, 32, 20, body);
                p.Rect(8, 34, 32, 4, Shade(body, 0.3f));
                p.Rect(28, 22, 9, 8, new Color(0.6f, 0.9f, 1f));
                p.Rect(12, 38, 6, 10, new Color(0.3f, 0.3f, 0.32f)); // exhaust
                // drill bit going into the ground
                p.Triangle(new Vector2(14, 10), new Vector2(34, 10), new Vector2(24, -2), steel);
                for (int i = 0; i < 4; i++) p.Line(15 + i * 5, 10 - i, 22 + i * 3, 3 - i, Shade(steel, -0.3f));
                p.Outline(new Color(0.08f, 0.08f, 0.1f));
                return p.ToSprite(16, new Vector2(0.5f, 0.05f));
            });
        }

        // ------------------------------------------------------------------ PETS
        public static Sprite Pet(PetDefinition def)
        {
            return Cached("pet_" + def.id, () =>
            {
                var p = new PixelCanvas(24, 24);
                var b = def.bodyColor;
                var acc = def.accentColor;
                switch (def.shape)
                {
                    case 1: // ears
                        p.Triangle(new Vector2(5, 14), new Vector2(10, 15), new Vector2(6, 22), acc);
                        p.Triangle(new Vector2(14, 15), new Vector2(19, 14), new Vector2(18, 22), acc);
                        break;
                    case 2: // wings
                        p.Ellipse(3.5f, 12, 3.5f, 6, Shade(b, 0.35f));
                        p.Ellipse(20.5f, 12, 3.5f, 6, Shade(b, 0.35f));
                        break;
                    case 3: // horns
                        p.Triangle(new Vector2(6, 15), new Vector2(9, 16), new Vector2(5, 23), new Color(1f, 0.95f, 0.8f));
                        p.Triangle(new Vector2(15, 16), new Vector2(18, 15), new Vector2(19, 23), new Color(1f, 0.95f, 0.8f));
                        break;
                    case 4: // tentacles
                        for (int i = 0; i < 4; i++) p.Rect(5 + i * 4, 1, 2, 5, acc);
                        break;
                }
                p.Ellipse(12, 10.5f, 8.5f, 8, b);
                p.Ellipse(12, 7, 6, 3.5f, Shade(b, 0.3f)); // belly
                p.Ellipse(10, 14, 3, 2, Shade(b, 0.25f));    // shine
                // eyes
                p.Rect(7, 10, 3, 4, Color.white);
                p.Rect(14, 10, 3, 4, Color.white);
                p.Rect(8, 10, 2, 3, new Color(0.08f, 0.06f, 0.1f));
                p.Rect(15, 10, 2, 3, new Color(0.08f, 0.06f, 0.1f));
                p.Set(8, 12, Color.white); p.Set(15, 12, Color.white);
                p.Rect(11, 7, 2, 1, Shade(b, -0.5f));
                p.Outline(Shade(b, -0.7f));
                return p.ToSprite(16, new Vector2(0.5f, 0f));
            });
        }

        public static Sprite Egg(Color c)
        {
            return Cached("egg_" + ColorUtility.ToHtmlStringRGB(c), () =>
            {
                var p = new PixelCanvas(24, 30);
                p.Ellipse(12, 13, 10, 12.5f, c);
                p.Ellipse(9, 18, 3, 4, Shade(c, 0.4f));
                var r = new System.Random(c.GetHashCode());
                for (int i = 0; i < 6; i++) p.Circle(5 + r.Next(14), 4 + r.Next(18), 1.5f + (float)r.NextDouble(), Shade(c, -0.3f));
                p.Outline(Shade(c, -0.7f));
                return p.ToSprite(16, new Vector2(0.5f, 0.5f));
            });
        }

        // ----------------------------------------------------------------- ICONS
        public static Sprite CoinIcon => Cached("ico_coin", () =>
        {
            var p = new PixelCanvas(32, 32);
            p.Circle(16, 16, 14, new Color(0.85f, 0.6f, 0.08f));
            p.Circle(16, 16, 12, new Color(1f, 0.82f, 0.2f));
            p.Circle(13, 19, 5, new Color(1f, 0.92f, 0.55f));
            p.Rect(15, 9, 3, 14, new Color(0.8f, 0.55f, 0.05f));
            p.Outline(new Color(0.45f, 0.3f, 0.02f));
            return p.ToSprite(32, new Vector2(0.5f, 0.5f));
        });

        public static Sprite GemIcon => Cached("ico_gem", () =>
        {
            var p = new PixelCanvas(32, 32);
            var g = new Color(1f, 0.35f, 0.8f);
            p.Triangle(new Vector2(3, 20), new Vector2(29, 20), new Vector2(16, 2), g);
            p.Triangle(new Vector2(3, 20), new Vector2(29, 20), new Vector2(16, 2), g);
            p.Rect(7, 20, 18, 7, Shade(g, 0.3f));
            p.Triangle(new Vector2(3, 20), new Vector2(7, 27), new Vector2(7, 20), Shade(g, 0.3f));
            p.Triangle(new Vector2(25, 20), new Vector2(25, 27), new Vector2(29, 20), Shade(g, 0.3f));
            p.Triangle(new Vector2(10, 20), new Vector2(16, 20), new Vector2(16, 6), Shade(g, 0.55f));
            p.Outline(Shade(g, -0.6f));
            return p.ToSprite(32, new Vector2(0.5f, 0.5f));
        });

        public static Sprite CrystalIcon => Cached("ico_crystal", () =>
        {
            var p = new PixelCanvas(32, 32);
            var c = new Color(0.3f, 0.95f, 1f);
            p.Triangle(new Vector2(16, 31), new Vector2(8, 22), new Vector2(24, 22), Shade(c, 0.4f));
            p.Rect(8, 9, 16, 13, c);
            p.Rect(8, 9, 6, 13, Shade(c, 0.35f));
            p.Triangle(new Vector2(8, 9), new Vector2(24, 9), new Vector2(16, 1), Shade(c, -0.25f));
            p.Outline(Shade(c, -0.65f));
            return p.ToSprite(32, new Vector2(0.5f, 0.5f));
        });

        public static Sprite StarIcon => Cached("ico_star", () =>
        {
            var p = new PixelCanvas(32, 32);
            var c = new Color(0.5f, 1f, 0.55f);
            for (int i = 0; i < 5; i++)
            {
                float a = Mathf.PI / 2 + i * Mathf.PI * 2 / 5;
                float a1 = a + Mathf.PI / 5, a2 = a - Mathf.PI / 5;
                p.Triangle(new Vector2(16 + Mathf.Cos(a) * 15, 16 + Mathf.Sin(a) * 15),
                    new Vector2(16 + Mathf.Cos(a1) * 6, 16 + Mathf.Sin(a1) * 6),
                    new Vector2(16 + Mathf.Cos(a2) * 6, 16 + Mathf.Sin(a2) * 6), c);
            }
            p.Circle(16, 16, 6.5f, c);
            p.Outline(Shade(c, -0.6f));
            return p.ToSprite(32, new Vector2(0.5f, 0.5f));
        });

        public static Sprite BagIcon => Cached("ico_bag", () =>
        {
            var p = new PixelCanvas(32, 32);
            var c = new Color(0.65f, 0.42f, 0.22f);
            p.Ellipse(16, 12, 12, 11, c);
            p.Rect(10, 21, 12, 4, Shade(c, -0.2f));
            p.Rect(12, 25, 8, 3, Shade(c, 0.15f));
            p.Ellipse(12, 14, 4, 5, Shade(c, 0.3f));
            p.Rect(13, 20, 6, 2, new Color(1f, 0.8f, 0.2f));
            p.Outline(Shade(c, -0.6f));
            return p.ToSprite(32, new Vector2(0.5f, 0.5f));
        });

        public static Sprite LockIcon => Cached("ico_lock", () =>
        {
            var p = new PixelCanvas(32, 32);
            var c = new Color(0.75f, 0.75f, 0.8f);
            p.Rect(6, 3, 20, 16, c);
            p.Rect(9, 19, 3, 7, c); p.Rect(20, 19, 3, 7, c); p.Rect(9, 25, 14, 3, c);
            p.Rect(14, 8, 4, 7, new Color(0.25f, 0.25f, 0.3f));
            p.Outline(new Color(0.2f, 0.2f, 0.25f));
            return p.ToSprite(32, new Vector2(0.5f, 0.5f));
        });

        public static Sprite PickaxeIcon(Color head) => Pickaxe(head);

        /// <summary>Right-pointing triangle (rotate for other directions).</summary>
        public static Sprite Arrow => Cached("ico_arrow", () =>
        {
            var p = new PixelCanvas(32, 32);
            p.Triangle(new Vector2(6, 3), new Vector2(6, 29), new Vector2(28, 16), Color.white);
            return p.ToSprite(32, new Vector2(0.5f, 0.5f), FilterMode.Bilinear);
        });

        public static Sprite UpArrow => Cached("ico_up", () =>
        {
            var p = new PixelCanvas(32, 32);
            p.Triangle(new Vector2(3, 8), new Vector2(29, 8), new Vector2(16, 28), Color.white);
            p.Rect(12, 2, 8, 8, Color.white);
            return p.ToSprite(32, new Vector2(0.5f, 0.5f), FilterMode.Bilinear);
        });

        public static Sprite IconFor(CurrencyType t)
        {
            switch (t)
            {
                case CurrencyType.Gems: return GemIcon;
                case CurrencyType.Crystals: return CrystalIcon;
                default: return CoinIcon;
            }
        }

        public static Sprite IconForStat(StatType s)
        {
            switch (s)
            {
                case StatType.MiningPower: case StatType.CritChance: case StatType.CritDamage: case StatType.MiningSpeed:
                case StatType.Splash: case StatType.MineRange:
                    return Pickaxe(new Color(0.75f, 0.78f, 0.85f));
                case StatType.Luck: case StatType.GemChance: return GemIcon;
                case StatType.BackpackCapacity: case StatType.OreMultiplier: case StatType.AutoSellBonus: case StatType.AutoSell:
                    return BagIcon;
                case StatType.OreValue: case StatType.CoinMultiplier: case StatType.StartingCoins: return CoinIcon;
                case StatType.XpMultiplier: case StatType.PetXp: return StarIcon;
                case StatType.CrystalGain: return CrystalIcon;
                default: return StarIcon;
            }
        }
    }
}
