using UnityEngine;

namespace IdleMiningEmpire
{
    /// <summary>A mineable rock: health bar, hit punch/flash, rarity glow, and boss label.</summary>
    public class Rock : MonoBehaviour
    {
        public Rarity rarity;
        public bool isBoss;
        public string bossName;
        public double maxHp, hp;
        public float radius = 1f;
        public ZoneDefinition zone;

        SpriteRenderer sr, glow, barBg, barFill;
        TextMesh label;
        float punch, flash, spawnT, hpBarShow, baseScale = 1f;
        Color baseColor = Color.white;

        public Vector3 Center => transform.position + Vector3.up * radius * 0.85f;
        public bool Dead => hp <= 0;

        public static Rock Create(GameManager gm, ZoneDefinition zone, Rarity rarity, bool boss, float x, double hp, string bossName = null)
        {
            var go = new GameObject(boss ? "Boss" : "Rock " + rarity);
            go.transform.position = new Vector3(x, 0, 0);
            var r = go.AddComponent<Rock>();
            r.zone = zone;
            r.rarity = rarity;
            r.isBoss = boss;
            r.bossName = bossName;
            r.maxHp = r.hp = hp;
            r.baseScale = boss ? 1.5f : 1f + (int)rarity * 0.06f;
            r.radius = (boss ? 1.4f : 0.85f) * r.baseScale;
            r.Build(gm);
            return r;
        }

        void Build(GameManager gm)
        {
            float lr = radius / baseScale;
            var oreColors = new Color[zone.oreIds.Length];
            for (int i = 0; i < oreColors.Length; i++)
            {
                var o = gm.Db.GetOre(zone.oreIds[i]);
                oreColors[i] = o != null ? o.color : Color.white;
            }

            if (rarity >= Rarity.Rare || isBoss)
            {
                glow = Child("Glow", SpriteFactory.SoftGlow, 9);
                glow.color = isBoss ? new Color(0.3f, 1f, 1f, 0.6f) : WithAlpha(PetManager.RarityColor(rarity), 0.55f);
                glow.transform.localPosition = new Vector3(0, lr * 0.8f, 0);
                glow.transform.localScale = Vector3.one * lr * (isBoss ? 4.5f : 3.4f);
            }

            sr = Child("Sprite", SpriteFactory.Rock(zone, oreColors, rarity, Random.Range(0, 3), isBoss), 10);
            if (rarity == Rarity.Mythic && !isBoss) sr.sortingOrder = 11;

            barBg = Child("HpBg", SpriteFactory.White, 12);
            barBg.color = new Color(0, 0, 0, 0.6f);
            barFill = Child("HpFill", SpriteFactory.White, 13);
            float bw = isBoss ? 3.4f : 1.5f;
            barBg.transform.localPosition = new Vector3(0, lr * 2.1f + 0.25f, 0);
            barBg.transform.localScale = new Vector3(bw + 0.08f, 0.2f, 1);
            barFill.transform.localScale = new Vector3(bw, 0.13f, 1);
            barBg.enabled = barFill.enabled = isBoss;

            if (isBoss || rarity >= Rarity.Epic)
            {
                var go = new GameObject("Label");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(0, lr * 2.1f + (isBoss ? 0.75f : 0.55f), 0);
                label = go.AddComponent<TextMesh>();
                label.font = UIFactory.DefaultFont;
                var mr = label.GetComponent<MeshRenderer>();
                mr.sharedMaterial = label.font.material;
                mr.sortingOrder = 14;
                label.fontSize = 64;
                label.characterSize = isBoss ? 0.07f : 0.05f;
                label.anchor = TextAnchor.MiddleCenter;
                label.fontStyle = FontStyle.Bold;
                label.text = isBoss ? bossName : rarity.ToString().ToUpperInvariant();
                label.color = isBoss ? new Color(0.4f, 1f, 1f) : PetManager.RarityColor(rarity);
            }

            transform.localScale = Vector3.zero;
            baseColor = sr.color;
        }

        static Color WithAlpha(Color c, float a) { c.a = a; return c; }

        SpriteRenderer Child(string name, Sprite s, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = s;
            r.sortingOrder = order;
            return r;
        }

        /// <summary>Applies damage. Returns true if this hit destroyed the rock.</summary>
        public bool Damage(double dmg)
        {
            if (Dead) return false;
            hp -= dmg;
            punch = 1f;
            flash = 1f;
            hpBarShow = 3f;
            return hp <= 0;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            spawnT = Mathf.Min(1f, spawnT + dt * 4f);
            float appear = 1f - Mathf.Pow(1f - spawnT, 3f);
            punch = Mathf.MoveTowards(punch, 0, dt * 7f);
            flash = Mathf.MoveTowards(flash, 0, dt * 9f);
            float sx = 1f + punch * 0.12f, sy = 1f - punch * 0.1f;
            transform.localScale = new Vector3(sx, sy, 1) * baseScale * appear;

            if (sr != null)
            {
                var c = Color.Lerp(baseColor, new Color(1f, 0.75f, 0.7f), flash * 0.8f);
                if (rarity == Rarity.Mythic && !isBoss) c = Color.Lerp(c, Color.HSVToRGB(Mathf.Repeat(Time.time * 0.25f, 1f), 0.35f, 1f), 0.45f);
                sr.color = c;
            }
            if (glow != null)
            {
                var g = glow.color;
                g.a = (isBoss ? 0.55f : 0.35f) + Mathf.Sin(Time.time * 3f + transform.position.x) * 0.15f;
                glow.color = g;
            }

            hpBarShow -= dt;
            bool showBar = isBoss || (hpBarShow > 0 && hp < maxHp);
            barBg.enabled = barFill.enabled = showBar;
            if (showBar)
            {
                float f = Mathf.Clamp01((float)(hp / maxHp));
                float bw = isBoss ? 3.4f : 1.5f;
                barFill.transform.localScale = new Vector3(bw * f, 0.13f, 1);
                barFill.transform.localPosition = barBg.transform.localPosition + new Vector3(-bw * (1 - f) / 2f, 0, 0);
                barFill.color = Color.Lerp(new Color(1f, 0.25f, 0.2f), new Color(0.3f, 1f, 0.4f), f);
            }
        }
    }
}
