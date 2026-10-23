using UnityEngine;

namespace IdleMiningEmpire
{
    /// <summary>An equipped pet that follows the player with a bouncy trail.</summary>
    public class PetFollower : MonoBehaviour
    {
        public PetInstance Instance { get; private set; }
        PlayerController owner;
        SpriteRenderer sr;
        int index;
        Rarity rarity;
        float sparkleTimer;

        public static PetFollower Create(PlayerController owner, PetInstance inst, PetDefinition def, int index)
        {
            var go = new GameObject("Pet " + def.displayName);
            var f = go.AddComponent<PetFollower>();
            f.owner = owner;
            f.Instance = inst;
            f.index = index;
            f.rarity = def.rarity;
            f.sr = go.AddComponent<SpriteRenderer>();
            f.sr.sprite = SpriteFactory.Pet(def);
            f.sr.sortingOrder = 29;
            go.transform.localScale = Vector3.one * (0.85f + (int)def.rarity * 0.08f);
            f.Snap();
            return f;
        }

        Vector3 Goal
        {
            get
            {
                var p = owner.transform.position;
                float behind = -owner.Facing * (1.2f + index * 0.9f);
                float fly = index % 2 == 1 ? 1.4f : 0f;
                return new Vector3(p.x + behind, p.y + fly, 0);
            }
        }

        public void Snap() { if (owner != null) transform.position = Goal; }

        void Update()
        {
            if (owner == null) { Destroy(gameObject); return; }
            var g = Goal;
            var p = transform.position;
            p.x = Mathf.Lerp(p.x, g.x, 1f - Mathf.Exp(-Time.deltaTime * (5f - index * 0.5f)));
            float baseY = Mathf.Lerp(p.y, g.y, 1f - Mathf.Exp(-Time.deltaTime * 6f));
            float hop = Mathf.Abs(Mathf.Sin(Time.time * 6f + index)) * 0.25f;
            p.y = baseY;
            transform.position = p;
            sr.transform.localPosition = Vector3.zero;
            sr.flipX = owner.transform.position.x < p.x;
            transform.localScale = new Vector3(transform.localScale.x, transform.localScale.x * (1f + hop * 0.15f), 1);

            if (rarity >= Rarity.Legendary)
            {
                sparkleTimer -= Time.deltaTime;
                if (sparkleTimer <= 0)
                {
                    sparkleTimer = 0.35f;
                    GameManager.Instance.Fx.Sparkle(p + Vector3.up * 0.6f, PetManager.RarityColor(rarity), 1, 0.4f);
                }
            }
        }
    }
}
