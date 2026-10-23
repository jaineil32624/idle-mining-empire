using UnityEngine;

namespace IdleMiningEmpire
{
    /// <summary>A tiny CPU-side pixel buffer used to paint procedural sprites.</summary>
    public class PixelCanvas
    {
        public readonly int W, H;
        public readonly Color32[] Px;

        public PixelCanvas(int w, int h)
        {
            W = w; H = h;
            Px = new Color32[w * h];
        }

        public bool In(int x, int y) => x >= 0 && y >= 0 && x < W && y < H;

        public void Set(int x, int y, Color c)
        {
            if (!In(x, y)) return;
            Px[y * W + x] = c;
        }

        public void Blend(int x, int y, Color c)
        {
            if (!In(x, y)) return;
            Color d = Px[y * W + x];
            float a = c.a + d.a * (1 - c.a);
            if (a <= 0.0001f) return;
            Color o = (c * c.a + d * d.a * (1 - c.a)) / a;
            o.a = a;
            Px[y * W + x] = o;
        }

        public Color Get(int x, int y) => In(x, y) ? (Color)Px[y * W + x] : Color.clear;
        public float Alpha(int x, int y) => In(x, y) ? Px[y * W + x].a / 255f : 0f;

        public void Rect(int x, int y, int w, int h, Color c)
        {
            for (int j = y; j < y + h; j++)
                for (int i = x; i < x + w; i++) Set(i, j, c);
        }

        public void Ellipse(float cx, float cy, float rx, float ry, Color c)
        {
            int x0 = Mathf.FloorToInt(cx - rx), x1 = Mathf.CeilToInt(cx + rx);
            int y0 = Mathf.FloorToInt(cy - ry), y1 = Mathf.CeilToInt(cy + ry);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
                    if (dx * dx + dy * dy <= 1f) Set(x, y, c);
                }
        }

        public void Circle(float cx, float cy, float r, Color c) => Ellipse(cx, cy, r, r, c);

        public void Line(float x0, float y0, float x1, float y1, Color c, int thick = 1)
        {
            float len = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
            int steps = Mathf.Max(1, Mathf.CeilToInt(len * 1.5f));
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                int x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
                int y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));
                if (thick <= 1) Set(x, y, c);
                else Rect(x - thick / 2, y - thick / 2, thick, thick, c);
            }
        }

        public void Triangle(Vector2 a, Vector2 b, Vector2 c, Color col)
        {
            int minX = Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x)));
            int maxX = Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x)));
            int minY = Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y)));
            int maxY = Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y)));
            for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float d1 = Sign(p, a, b), d2 = Sign(p, b, c), d3 = Sign(p, c, a);
                    bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
                    if (!(neg && pos)) Set(x, y, col);
                }
        }

        static float Sign(Vector2 p1, Vector2 p2, Vector2 p3) =>
            (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);

        /// <summary>Draws a 1px darker outline around all opaque pixels.</summary>
        public void Outline(Color c)
        {
            var copy = (Color32[])Px.Clone();
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    if (copy[y * W + x].a > 0) continue;
                    bool edge = false;
                    for (int k = 0; k < 4 && !edge; k++)
                    {
                        int nx = x + (k == 0 ? 1 : k == 1 ? -1 : 0), ny = y + (k == 2 ? 1 : k == 3 ? -1 : 0);
                        if (nx >= 0 && ny >= 0 && nx < W && ny < H && copy[ny * W + nx].a > 128) edge = true;
                    }
                    if (edge) Px[y * W + x] = c;
                }
        }

        public Texture2D ToTexture(FilterMode filter = FilterMode.Point, TextureWrapMode wrap = TextureWrapMode.Clamp)
        {
            var t = new Texture2D(W, H, TextureFormat.RGBA32, false);
            t.filterMode = filter;
            t.wrapMode = wrap;
            t.SetPixels32(Px);
            t.Apply(false, true);
            return t;
        }

        public Sprite ToSprite(float ppu, Vector2 pivot, FilterMode filter = FilterMode.Point,
            TextureWrapMode wrap = TextureWrapMode.Clamp, Vector4 border = default)
        {
            var t = ToTexture(filter, wrap);
            return Sprite.Create(t, new Rect(0, 0, W, H), pivot, ppu, 0, SpriteMeshType.FullRect, border);
        }
    }
}
