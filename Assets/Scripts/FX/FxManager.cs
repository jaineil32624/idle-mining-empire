using System.Collections.Generic;
using UnityEngine;

namespace IdleMiningEmpire
{
    /// <summary>Lightweight pooled particles, floating damage numbers, screen shake and flashes.</summary>
    public class FxManager : MonoBehaviour
    {
        class Particle
        {
            public SpriteRenderer sr;
            public Vector2 vel;
            public float life, maxLife, gravity, spin, startScale;
            public bool fade = true;
        }

        class FloatText
        {
            public TextMesh tm;
            public MeshRenderer mr;
            public Vector3 vel;
            public float life, maxLife, scale;
            public Color color;
        }

        GameManager gm;
        Transform root;
        readonly List<Particle> active = new List<Particle>();
        readonly Stack<Particle> freeParticles = new Stack<Particle>();
        readonly List<FloatText> texts = new List<FloatText>();
        readonly Stack<FloatText> freeTexts = new Stack<FloatText>();
        float shake;
        public Font Font { get; private set; }
        int MaxParticles => gm != null && gm.Save != null && gm.Save.lowFx ? 120 : 400;

        public void Init(GameManager g)
        {
            gm = g;
            root = new GameObject("FX").transform;
            Font = UIFactory.DefaultFont;
        }

        // ---------------------------------------------------------- Particles
        Particle GetParticle()
        {
            Particle p;
            if (freeParticles.Count > 0) p = freeParticles.Pop();
            else
            {
                var go = new GameObject("p");
                go.transform.SetParent(root, false);
                p = new Particle { sr = go.AddComponent<SpriteRenderer>() };
                p.sr.sortingOrder = 50;
            }
            p.sr.gameObject.SetActive(true);
            return p;
        }

        public void Burst(Vector3 pos, Color color, int count, float speed = 6f, float size = 0.18f, float gravity = 18f, float life = 0.6f, Sprite sprite = null)
        {
            if (gm.Save.lowFx) count = Mathf.Max(1, count / 3);
            for (int i = 0; i < count && active.Count < MaxParticles; i++)
            {
                var p = GetParticle();
                p.sr.sprite = sprite != null ? sprite : SpriteFactory.White;
                var c = color;
                c.r *= Random.Range(0.8f, 1.15f); c.g *= Random.Range(0.8f, 1.15f); c.b *= Random.Range(0.8f, 1.15f);
                p.sr.color = c;
                float s = size * Random.Range(0.6f, 1.4f);
                p.startScale = s;
                p.sr.transform.position = pos + (Vector3)(Random.insideUnitCircle * 0.25f);
                p.sr.transform.localScale = Vector3.one * s;
                p.sr.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0, 360f));
                float a = Random.Range(0.15f, Mathf.PI - 0.15f);
                p.vel = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * speed * Random.Range(0.4f, 1.1f);
                p.gravity = gravity;
                p.spin = Random.Range(-720f, 720f);
                p.life = p.maxLife = life * Random.Range(0.7f, 1.3f);
                p.fade = true;
                active.Add(p);
            }
        }

        public void Sparkle(Vector3 pos, Color color, int count = 6, float radius = 1f)
        {
            for (int i = 0; i < count && active.Count < MaxParticles; i++)
            {
                var p = GetParticle();
                p.sr.sprite = SpriteFactory.SoftGlow;
                p.sr.color = color;
                p.startScale = Random.Range(0.25f, 0.5f);
                p.sr.transform.position = pos + (Vector3)(Random.insideUnitCircle * radius);
                p.sr.transform.localScale = Vector3.one * p.startScale;
                p.vel = new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(0.5f, 1.5f));
                p.gravity = 0;
                p.spin = 0;
                p.life = p.maxLife = Random.Range(0.6f, 1.2f);
                active.Add(p);
            }
        }

        public void Confetti()
        {
            var cam = gm.Cam != null ? gm.Cam.Camera : null;
            if (cam == null) return;
            var top = cam.transform.position + new Vector3(0, cam.orthographicSize * 0.6f, 10);
            Color[] cols = { new Color(1, 0.3f, 0.4f), new Color(1, 0.85f, 0.2f), new Color(0.3f, 0.9f, 0.5f), new Color(0.3f, 0.6f, 1), new Color(0.9f, 0.4f, 1) };
            for (int i = 0; i < 5; i++)
                Burst(top + new Vector3(Random.Range(-6f, 6f), 0, 0), cols[i], 10, 7f, 0.16f, 9f, 1.4f);
        }

        // ------------------------------------------------------- Floating text
        public void FloatingText(Vector3 pos, string text, Color color, float scale = 1f)
        {
            FloatText f;
            if (freeTexts.Count > 0) f = freeTexts.Pop();
            else
            {
                var go = new GameObject("txt");
                go.transform.SetParent(root, false);
                f = new FloatText { tm = go.AddComponent<TextMesh>() };
                f.mr = go.GetComponent<MeshRenderer>();
                f.tm.font = Font;
                f.mr.sharedMaterial = Font.material;
                f.mr.sortingOrder = 60;
                f.tm.anchor = TextAnchor.MiddleCenter;
                f.tm.alignment = TextAlignment.Center;
                f.tm.fontSize = 64;
                f.tm.characterSize = 0.085f;
                f.tm.fontStyle = FontStyle.Bold;
            }
            if (texts.Count > 40) Recycle(texts[0]);
            f.tm.gameObject.SetActive(true);
            f.tm.text = text;
            f.color = color;
            f.tm.color = color;
            f.scale = scale;
            f.tm.transform.position = new Vector3(pos.x + Random.Range(-0.25f, 0.25f), pos.y, -1);
            f.tm.transform.localScale = Vector3.one * scale * 1.4f;
            f.vel = new Vector3(Random.Range(-0.4f, 0.4f), 2.6f, 0);
            f.life = f.maxLife = 0.9f;
            texts.Add(f);
        }

        void Recycle(FloatText f)
        {
            texts.Remove(f);
            f.tm.gameObject.SetActive(false);
            freeTexts.Push(f);
        }

        // ------------------------------------------------------------ Shake etc
        public void Shake(float amount) { shake = Mathf.Max(shake, amount); }

        public Vector3 ShakeOffset { get; private set; }

        public void Flash(Color c)
        {
            if (gm.UI != null) gm.UI.ScreenFlash(c);
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var p = active[i];
                p.life -= dt;
                if (p.life <= 0)
                {
                    p.sr.gameObject.SetActive(false);
                    active.RemoveAt(i);
                    freeParticles.Push(p);
                    continue;
                }
                p.vel.y -= p.gravity * dt;
                var t = p.sr.transform;
                var pos = t.position + (Vector3)(p.vel * dt);
                if (p.gravity > 0 && pos.y < 0.05f) { pos.y = 0.05f; p.vel.y *= -0.35f; p.vel.x *= 0.6f; }
                t.position = pos;
                t.Rotate(0, 0, p.spin * dt);
                float k = p.life / p.maxLife;
                if (p.fade)
                {
                    var c = p.sr.color; c.a = Mathf.Clamp01(k * 1.5f); p.sr.color = c;
                    t.localScale = Vector3.one * p.startScale * Mathf.Lerp(0.4f, 1f, k);
                }
            }

            for (int i = texts.Count - 1; i >= 0; i--)
            {
                var f = texts[i];
                f.life -= dt;
                if (f.life <= 0) { Recycle(f); continue; }
                f.vel.y = Mathf.Max(0.3f, f.vel.y - 4f * dt);
                f.tm.transform.position += f.vel * dt;
                float k = f.life / f.maxLife;
                float pop = k > 0.85f ? Mathf.Lerp(1.4f, 1f, (1 - k) / 0.15f) : 1f;
                f.tm.transform.localScale = Vector3.one * f.scale * pop;
                var c = f.color; c.a = Mathf.Clamp01(k * 2.5f); f.tm.color = c;
            }

            if (shake > 0.001f)
            {
                ShakeOffset = (Vector3)(Random.insideUnitCircle * shake);
                shake = Mathf.MoveTowards(shake, 0, dt * 2.5f);
            }
            else ShakeOffset = Vector3.zero;
        }
    }
}
